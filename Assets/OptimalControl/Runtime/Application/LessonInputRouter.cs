using System;
using System.Collections.Generic;
using System.Globalization;
using OptCon.Core;
using OptCon.Presentation;
using OptCon.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OptCon.Application
{
    /// <summary>Lesson-local keyboard adapter; issues session commands and never owns mathematical state.</summary>
    public sealed class LessonInputRouter : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private int controlComponent;
        [SerializeField] private string controlUnit = "N·m";
        [SerializeField] private double controlIncrement = 0.5;
        [SerializeField] private InputEditScope editScope = InputEditScope.Tail;
        [SerializeField] private LayerController layers;
        [SerializeField] private TextMesh controlHint;
        private SimulationSession session;
        private InputActionAsset runtimeActions;
        private Func<string, bool> blocksShortcut;
        public void SetFocusGuard(Func<string, bool> guard) => blocksShortcut = guard;
        private readonly List<KeyValuePair<InputAction, Action<InputAction.CallbackContext>>> handlers =
            new List<KeyValuePair<InputAction, Action<InputAction.CallbackContext>>>();
        public SimulationSession BoundSession => session;
        public InputActionAsset RuntimeActions => runtimeActions;

        public void Bind(SimulationSession value)
        {
            Disconnect(); session = value;
            if (value == null) return;
            NumericValidation.RequirePositive(controlIncrement, nameof(controlIncrement));
            var model = value.State.Result.Configuration.Model.Descriptor;
            if (controlComponent < 0 || controlComponent >= model.Dimensions.Control || model.ControlUnits[controlComponent] != controlUnit)
                throw new ArgumentException("Input adapter component/unit does not match the model.");
            if (actions == null) throw new ArgumentException("Dedicated lesson input actions are required.");
            runtimeActions = Instantiate(actions);
            Hook("TogglePlayback", _ => session.Execute(session.State.IsPlaying ? SimulationCommand.Pause() : SimulationCommand.Play()));
            Hook("StepForward", _ => session.Execute(SimulationCommand.StepForward()));
            Hook("StepBackward", _ => session.Execute(SimulationCommand.StepBackward()));
            Hook("Reset", _ => session.Execute(SimulationCommand.Reset()));
            Hook("IncreaseControl", _ => ChangeControl(1));
            Hook("DecreaseControl", _ => ChangeControl(-1));
            Hook("ZeroControl", _ => ChangeControl(0));
            Hook("ToggleBody", _ => layers.Toggle(0));
            Hook("TogglePhase", _ => layers.Toggle(1));
            Hook("ToggleTrajectory", _ => layers.Toggle(2));
            if (controlHint != null)
            {
                string Key(string name) => runtimeActions.FindAction(name, true).GetBindingDisplayString();
                controlHint.text = Key("TogglePlayback") + ": play/pausa   " + Key("StepBackward") + "/" + Key("StepForward") + ": step   " + Key("Reset") + ": reset\n" +
                    Key("DecreaseControl") + "/" + Key("IncreaseControl") + ": input ±" + controlIncrement.ToString("G4", CultureInfo.InvariantCulture) + " " + controlUnit +
                    (editScope == InputEditScope.Tail ? " sulla coda da t" : " sul campione t") + "   " + Key("ZeroControl") + ": azzera\n" +
                    Key("ToggleBody") + "/" + Key("TogglePhase") + "/" + Key("ToggleTrajectory") + ": layer corpo / fase / stato-tempo";
            }
            if (isActiveAndEnabled) runtimeActions.Enable();
        }
        private void Hook(string name, Action<InputAction.CallbackContext> handler)
        {
            var command = handler;
            handler = context => { if (blocksShortcut == null || !blocksShortcut(name)) command(context); };
            var action = runtimeActions.FindAction(name, true);
            handlers.Add(new KeyValuePair<InputAction, Action<InputAction.CallbackContext>>(action, handler));
            action.performed += handler;
        }
        private void ChangeControl(int direction)
        {
            var state = session.State;
            if (state.Sample.IsTerminal) return;
            // Input edits operate on the requested sequence; displayed results always use Result.Configuration.
            var values = state.RequestedConfiguration.Controls[state.Sample.Timestep].ToArray();
            values[controlComponent] = direction == 0 ? 0 : values[controlComponent] + direction * controlIncrement;
            if (NumericValidation.IsFinite(values[controlComponent]))
                session.Execute(SimulationCommand.SetInput(state.Sample.Timestep, new NumericVector(values), editScope));
        }
        private void Disconnect()
        {
            if (runtimeActions != null) runtimeActions.Disable();
            foreach (var handler in handlers) handler.Key.performed -= handler.Value;
            handlers.Clear();
            if (runtimeActions != null) Destroy(runtimeActions);
            runtimeActions = null;
            blocksShortcut = null;
        }
        private void OnEnable() { if (session != null && runtimeActions != null) runtimeActions.Enable(); }
        private void OnDisable() { if (runtimeActions != null) runtimeActions.Disable(); }
        private void OnDestroy() { Disconnect(); session = null; }
    }
}

using System;
using OptCon.Simulation;
using UnityEngine.UIElements;

namespace OptCon.Presentation
{
    /// <summary>Presentation timeline commands; playback rate is explicitly not the physical step.</summary>
    public sealed class TimelinePresenter : IDisposable
    {
        private readonly VisualElement root;
        private readonly ICommandSink commands;
        private readonly SliderInt seek;
        private readonly DoubleField rate;
        private readonly Button play, back, next, reset;
        private bool playing;
        private double displayedRate;
        private bool rendering;
        public TimelinePresenter(VisualElement root, ICommandSink commands)
        {
            this.root = root; this.commands = commands;
            seek = root.Q<SliderInt>("timeline-seek"); rate = root.Q<DoubleField>("playback-rate");
            play = root.Q<Button>("play-pause"); back = root.Q<Button>("step-back"); next = root.Q<Button>("step-next"); reset = root.Q<Button>("reset");
            play.clicked += Toggle; back.clicked += Back; next.clicked += Next; reset.clicked += Reset;
            seek.RegisterValueChangedCallback(Seek); rate.RegisterValueChangedCallback(Rate);
        }
        private void Toggle() => commands.Execute(playing ? SimulationCommand.Pause() : SimulationCommand.Play());
        private void Back() => commands.Execute(SimulationCommand.StepBackward());
        private void Next() => commands.Execute(SimulationCommand.StepForward());
        private void Reset() => commands.Execute(SimulationCommand.Reset());
        private void Seek(ChangeEvent<int> e) { if (!rendering) commands.Execute(SimulationCommand.Seek(e.newValue)); }
        private void Rate(ChangeEvent<double> e)
        {
            if (!commands.Execute(SimulationCommand.SetPlaybackRate(e.newValue)))
            {
                rate.SetValueWithoutNotify(displayedRate);
                root.Q<Label>("timeline-status").text += "\nPlayback rifiutato: usare campioni/s positivi e finiti.";
            }
        }
        public void Render(SessionState state)
        {
            playing = state.IsPlaying; play.text = playing ? "Pausa" : "Play";
            play.SetEnabled(playing || (!state.Sample.IsTerminal && state.Error == null));
            back.SetEnabled(state.Sample.Timestep > 0); next.SetEnabled(!state.Sample.IsTerminal);
            // Range changes can clamp SliderInt.value and emit ChangeEvent. Rendering must never issue commands.
            rendering = true;
            try { seek.highValue = state.Sample.Horizon; seek.SetValueWithoutNotify(state.Sample.Timestep); }
            finally { rendering = false; }
            displayedRate = state.SamplesPerSecond; rate.SetValueWithoutNotify(displayedRate);
            root.Q<Label>("timeline-status").text = "r" + state.Result.RevisionId + " · t=" + state.Sample.Timestep + "/" + state.Sample.Horizon +
                (state.Sample.IsTerminal ? " · terminale: nessuna transizione" : " · campione discreto") +
                (state.Error == null ? "" : "\nTentativo r" + state.AttemptRevisionId + " rifiutato: " + state.Error);
        }
        public void Dispose()
        {
            play.clicked -= Toggle; back.clicked -= Back; next.clicked -= Next; reset.clicked -= Reset;
            seek.UnregisterValueChangedCallback(Seek); rate.UnregisterValueChangedCallback(Rate);
        }
    }
}

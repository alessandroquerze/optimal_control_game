using System;
using System.Globalization;
using System.Linq;
using OptCon.Core;
using OptCon.Simulation;
using UnityEngine;
namespace OptCon.Presentation
{
    /// <summary>Geometric planar pose and two force glyphs. Reads only the published result; no dynamics.</summary>
    public sealed class PlanarDroneBodyView : MonoBehaviour
    {
        [SerializeField] private StateProjection position;
        [SerializeField] private LineRenderer body, corridor;
        [SerializeField] private VectorRenderer thrust1, thrust2;
        [SerializeField] private TextMesh label;
        [SerializeField] private double forceScale = .12;
        public SessionState DisplayedState { get; private set; }
        public void Render(SessionState state)
        {
            DisplayedState=state; var result=state.Result; var model=result.Configuration.Model as PlanarDroneConfiguration;
            if(model==null) throw new ArgumentException("Planar pose requires the planar drone model.");
            var x=state.Sample.CurrentState; var p=position.Project(result.History,state.Sample.Timestep);
            double c=Math.Cos(x[4]), s=Math.Sin(x[4]); float half=StateProjection.ToFloat(model.ArmLength*.5*position.X.scale);
            var arm=new Vector3(StateProjection.ToFloat(c),StateProjection.ToFloat(s),0)*half;
            body.useWorldSpace=false; body.positionCount=3; body.SetPositions(new[] {p-arm,p,p+arm});
            var u=state.Sample.AppliedControl;
            Vector3 Force(int i) => u==null ? Vector3.zero : new Vector3(StateProjection.ToFloat(-s*u[i]),StateProjection.ToFloat(c*u[i]),0);
            thrust1.Render(p+arm,Force(0),"N",forceScale); thrust2.Render(p-arm,Force(1),"N",forceScale);
            foreach(var target in body.GetComponents<HighlightTarget>()) target.SetSample(result.RevisionId,state.Sample.Timestep);
            foreach(var target in thrust1.GetComponents<HighlightTarget>()) target.SetSample(result.RevisionId,u==null ? (int?)null : state.Sample.Timestep);
            foreach(var target in thrust2.GetComponents<HighlightTarget>()) target.SetSample(result.RevisionId,u==null ? (int?)null : state.Sample.Timestep);
            var quota=result.Configuration.Constraints?.Path.FirstOrDefault(b=>b.IsState && b.Component==2 && b.IsInterval);
            corridor.useWorldSpace=false; corridor.positionCount=0;
            if(quota!=null) { float lo=StateProjection.ToFloat(position.Y.offset+position.Y.scale*quota.Minimum),hi=StateProjection.ToFloat(position.Y.offset+position.Y.scale*quota.Maximum);
                corridor.positionCount=5; corridor.SetPositions(new[] {new Vector3(-1,lo,0),new Vector3(2,lo,0),new Vector3(2,hi,0),new Vector3(-1,hi,0),new Vector3(-1,lo,0)}); }
            string N(double value)=>value.ToString("G4",CultureInfo.InvariantCulture);
            label.text="DRONE PLANARE · (p_x,p_y) [m]; θ="+N(x[4])+" rad\n"+
                "Velocità omesse: v_x,v_y,ω · r"+result.RevisionId+" · t="+state.Sample.Timestep+"\n"+
                "u₁ dx / u₂ sx [N]: "+(u==null ? "N/A" : "["+N(u[0])+", "+N(u[1])+"]")+
                " · frecce "+N(forceScale)+" unità scena/N\n"+
                "Quota diagnostica · reale linea / desiderato tratteggio";
        }
    }
}

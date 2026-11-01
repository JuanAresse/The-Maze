#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class MazeBuild
{
    public static void VerifyAndBuild()
    {
        try
        {
            VerifyPlans();
            string root=Directory.GetParent(Application.dataPath).Parent.FullName;
            string output=Path.Combine(root,"ejecutable_actualizado","theMaze.exe");
            string[] arguments=Environment.GetCommandLineArgs();
            int outputIndex=Array.IndexOf(arguments,"-maze-build-output");
            if(outputIndex>=0&&outputIndex+1<arguments.Length)output=Path.GetFullPath(arguments[outputIndex+1]);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=new[]{"Assets/Scenes/MENU.unity","Assets/Scenes/The maze.unity"},
                locationPathName=output,target=BuildTarget.StandaloneWindows64,
                options=Array.IndexOf(arguments,"-maze-release")>=0?BuildOptions.None:BuildOptions.Development
            });
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("La compilación falló: "+report.summary.result);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(output),"VERIFICACION_COMPILACION.txt"),"Compilación correcta. Pruebas de planes: presupuesto, repeticiones, disparos, ciclos y reordenamiento.\n"+DateTime.UtcNow.ToString("O"));
            Debug.Log("MAZE_BUILD_OK "+output);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }

    private static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static void VerifyPlans()
    {
        var repeat=new ActionBlock(ActionKind.Repeat){Repetitions=4};
        repeat.Children.Add(new ActionBlock(ActionKind.Up));repeat.Children.Add(new ActionBlock(ActionKind.Right));
        var plan=new List<ActionBlock>{repeat};
        Check(ActionPlan.Validate(plan).IsValid&&ActionPlan.Validate(plan).Steps.Count==8,"Repeat debe consumir ocho pasos.");
        plan.Add(new ActionBlock(ActionKind.Wait));Check(!ActionPlan.Validate(plan).IsValid,"No debe permitir nueve acciones.");
        var shootRepeat=new ActionBlock(ActionKind.Repeat){Repetitions=2};shootRepeat.Children.Add(new ActionBlock(ActionKind.Shoot));
        Check(!ActionPlan.Validate(new List<ActionBlock>{shootRepeat}).IsValid,"Repeat no debe eludir el límite de disparos.");
        var outer=new ActionBlock(ActionKind.Repeat){Repetitions=2};var inner=new ActionBlock(ActionKind.Repeat){Repetitions=2};inner.Children.Add(new ActionBlock(ActionKind.Up));outer.Children.Add(inner);
        var valid=ActionPlan.Validate(new List<ActionBlock>{outer});Check(valid.IsValid&&valid.Actions==4&&valid.Steps[0].Parents.Count==2,"Repeticiones anidadas incorrectas.");
        Check(!ActionPlan.Validate(new List<ActionBlock>{new ActionBlock(ActionKind.Repeat)}).IsValid,"Repetir vacío debe invalidarse.");
        Check(!MovementBoard.MoveBlock(new List<ActionBlock>{outer},outer,inner.Children,inner,0),"No se puede arrastrar un padre a su descendiente.");
        var first=new ActionBlock(ActionKind.Up);var second=new ActionBlock(ActionKind.Down);var list=new List<ActionBlock>{first,second};
        Check(MovementBoard.MoveBlock(list,first,list,null,2)&&list[1]==first,"Mover dentro de la misma fila alteró el orden.");
        var destination=new List<ActionBlock>();Check(MovementBoard.MoveBlock(list,first,destination,null,0)&&destination[0]==first&&list.Count==1,"Mover entre filas debe trasladar, sin duplicar.");
        inner.Children.Add(outer);Check(!ActionPlan.Validate(new List<ActionBlock>{outer}).IsValid,"Las referencias cíclicas deben rechazarse.");
        Debug.Log("MAZE_PLAN_TESTS_OK");
    }
}
#endif

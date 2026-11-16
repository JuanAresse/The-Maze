#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Opt-in integration checks; never runs in an ordinary game session.
public class MazeSmokeTest : MonoBehaviour
{
    private readonly List<string> _checks=new List<string>();
    private readonly List<string> _errors=new List<string>();
    private string _output;
    private bool _finished;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-maze-smoke-test")<0)return;
        if(FindFirstObjectByType<MazeSmokeTest>()!=null)return;
        Application.runInBackground=true;
        var test=new GameObject("IntegrationChecks").AddComponent<MazeSmokeTest>();DontDestroyOnLoad(test.gameObject);
        test._output=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"verificacion");Directory.CreateDirectory(test._output);
        Application.logMessageReceived+=test.Log;
        test.StartCoroutine(test.Guard());
    }
    private void Log(string message,string trace,LogType type)
    {if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)_errors.Add(message+"\n"+trace);}
    private void Check(bool success,string name){if(!success)throw new Exception(name);_checks.Add(name);}
    private IEnumerator Guard()
    {
        var routine=Run();
        while(true)
        {
            bool next=false;object value=null;Exception error=null;
            try{next=routine.MoveNext();if(next)value=routine.Current;}catch(Exception e){error=e;}
            if(error!=null){Finish(error.ToString());yield break;}
            if(!next){Finish(null);yield break;}
            yield return value;
        }
    }
    private T Find<T>() where T:UnityEngine.Object=>FindFirstObjectByType<T>();
    private void Click(string name)
    {
        var go=GameObject.Find(name);Check(go!=null,"Existe control "+name);
        var button=go.GetComponent<Button>();Check(button!=null&&button.interactable,"Control habilitado "+name);button.onClick.Invoke();
    }
    private MazeCell[,] Grid(MazeGenerator generator)=>(MazeCell[,])typeof(MazeGenerator).GetField("_mazeGrid",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(generator);
    private bool Inside(MazeCell[,] cells,Vector2Int p)=>p.x>=0&&p.y>=0&&p.x<cells.GetLength(0)&&p.y<cells.GetLength(1);
    private bool Exit(MazeCell[,] cells,Vector2Int p)=>cells[p.x,p.y].GetComponentInChildren<ExitZone>()!=null;
    private bool Wall(MazeCell cell,Vector2Int dir)=>dir==Vector2Int.up?cell.HasFrontWall():dir==Vector2Int.down?cell.HasBackWall():dir==Vector2Int.left?cell.HasLeftWall():cell.HasRightWall();

    private IEnumerator Run()
    {
        while(!UnityEngine.Rendering.SplashScreen.isFinished)yield return null;
        yield return new WaitForSecondsRealtime(.7f);
        Find<ControlMenu>().OpenInstructions();
        yield return null;
        CheckInstructions();
        ScreenCapture.CaptureScreenshot(Path.Combine(_output,"instrucciones-menu.png"));
        yield return new WaitForEndOfFrame();
        Find<ControlMenu>().BackToMainMenu();
        Find<ControlMenu>().Play();yield return null;
        Check(Find<MazeSetupDialog>()!=null,"El menú permite elegir tamaño antes de jugar");
        ScreenCapture.CaptureScreenshot(Path.Combine(_output,"selector-tamano.png"));yield return new WaitForEndOfFrame();yield return new WaitForSecondsRealtime(.15f);
        Click("Size_8");Click("StartMatch");yield return new WaitForSecondsRealtime(1);
        foreach(int size in new[]{8,4,6})
        {
            if(size!=8){GameOptions.ChooseSize(size);UnityEngine.Random.InitState(size);SceneManager.LoadScene("The maze");yield return new WaitForSecondsRealtime(.7f);}
            var cells=Grid(Find<MazeGenerator>());Check(cells.GetLength(0)==size&&cells.GetLength(1)==size,"Generación "+size+"x"+size);
            var visited=new HashSet<Vector2Int>{Vector2Int.zero};var queue=new Queue<Vector2Int>();queue.Enqueue(Vector2Int.zero);
            var directions=new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
            while(queue.Count>0){var p=queue.Dequeue();foreach(var dir in directions){var next=p+dir;if(Inside(cells,next)&&!Wall(cells[p.x,p.y],dir)&&visited.Add(next))queue.Enqueue(next);}}
            Check(visited.Count==size*size,"Todas las casillas conectadas "+size+"x"+size);
        }
        var manager=Find<TurnBasedManager>();manager.StepDelay=.03f;
        var board=Find<MovementBoard>();
        Check(manager.CurrentTurn==0,"Comienza A");
        CheckFirstTurnShotBlocked(manager, board, false);
        manager.CurrentPlan.Clear();
        board.Add(ActionKind.Wait);
        manager.Execute();
        while(manager.IsExecuting)yield return null;
        Check(manager.CurrentTurn==1,"Ejecutar la primera ruta cambia al turno de B");
        CheckFirstTurnShotBlocked(manager, board, true);
        manager.ToggleContinuous();
        yield return null;
        yield return null;
        Check(!manager.Continuous&&!manager.IsExecuting&&manager.CurrentTurn==1,"Continuo rechaza el disparo de primer turno");
        manager.CurrentPlan.Clear();
        manager.PassTurn();
        Check(manager.CurrentTurn==0&&manager.CanShootThisTurn&&GameObject.Find("Palette_Shoot").GetComponent<Button>().interactable,"A puede disparar en su segundo turno");
        manager.PassTurn();
        Check(manager.CurrentTurn==1&&manager.CanShootThisTurn&&GameObject.Find("Palette_Shoot").GetComponent<Button>().interactable,"B puede disparar en su segundo turno tras pasar el primero");
        manager.PassTurn();
        manager.CurrentPlan.Clear();
        board.Refresh();
        foreach(var powerup in FindObjectsByType<Powerup>(FindObjectsSortMode.None))
            Check(powerup.GetComponent<Renderer>().sharedMaterial.shader.name=="Universal Render Pipeline/Lit","Material de poder compatible con URP: "+powerup.Type);
        Check(GameObject.Find("ExitMarker").GetComponent<Renderer>().sharedMaterial.color.g>.6f,"La salida tiene una señal verde");
        Check(Camera.allCameras.Length==1,"Una sola cámara activa");Check(FindObjectsByType<MovementBoard>(FindObjectsSortMode.None).Length==1,"Un solo panel de movimientos");
        Check(Camera.allCameras[0].rect==new Rect(0,0,1,1),"La cámara ocupa toda la pantalla");
        Check(Camera.allCameras[0].GetComponent<CamaraTerceraPersona>().objetivo==manager.CurrentPlayer.transform,"La cámara sigue al jugador activo");
        float time=manager.TimeRemaining;Find<ControlMenu>().Pause();yield return new WaitForSecondsRealtime(.2f);
        Check(Mathf.Abs(manager.TimeRemaining-time)<.01f,"La pausa detiene el tiempo de edición");Find<ControlMenu>().Resume();yield return null;
        Find<ControlMenu>().Pause();
        Find<ControlMenu>().OpenInstructionsFromPause();
        yield return null;
        CheckInstructions();
        ScreenCapture.CaptureScreenshot(Path.Combine(_output,"instrucciones-pausa.png"));
        yield return new WaitForEndOfFrame();
        Find<ControlMenu>().Resume();
        // Native pointer drag from the palette into the repeat editor, including reorder.
        board.Add(ActionKind.Repeat);yield return null;
        var palette=GameObject.Find("Palette_Up").GetComponent<ActionDrag>();
        var zone=GameObject.Find("RepeatSequence").GetComponent<ScrollRect>().viewport;
        var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,zone.TransformPoint(zone.rect.center))};
        board.BeginDrag(palette,pointer);board.Drag(pointer);board.EndDrag(pointer);yield return null;
        Check(manager.CurrentPlan[0].Children.Count==1,"Arrastrar desde paleta a repetición");
        var repeatIcon=GameObject.Find("RepeatSequence").GetComponentInChildren<ActionIcon>();
        Check(repeatIcon!=null&&repeatIcon.GetComponent<CanvasRenderer>()!=null,"El icono del bloque tiene CanvasRenderer dentro de la zona recortada");
        Canvas.ForceUpdateCanvases();
        var remove=GameObject.Find("Block_0_Repeat").transform.Find("Remove").GetComponent<Button>();
        Check(remove.GetComponentInChildren<Text>().rectTransform.rect.height>=remove.GetComponentInChildren<Text>().fontSize*1.15f,"El botón de eliminar tiene espacio para mostrar su icono");
        remove.onClick.Invoke();Check(manager.CurrentPlan.Count==0,"Eliminar un bloque desde la interfaz");
        var parent=new ActionBlock(ActionKind.Repeat);var child=new ActionBlock(ActionKind.Shoot);parent.Children.Add(child);manager.CurrentPlan.Add(parent);board.Refresh();board.OpenEditor(child);
        GameObject.Find("Block_0_Repeat").transform.Find("Remove").GetComponent<Button>().onClick.Invoke();Check(GameObject.Find("ActionEditor")==null,"Eliminar el padre cierra el editor de un descendiente");
        manager.CurrentPlan.Clear();var repeat=new ActionBlock(ActionKind.Repeat){Repetitions=4};repeat.Children.Add(new ActionBlock(ActionKind.Up));repeat.Children.Add(new ActionBlock(ActionKind.Right));manager.CurrentPlan.Add(repeat);
        Check(ActionPlan.Validate(manager.CurrentPlan).Actions==8,"Repetir cuenta todos los pasos");manager.CurrentPlan.Add(new ActionBlock(ActionKind.Wait));manager.Execute();Check(!manager.IsExecuting,"Se rechazan más de ocho acciones sin truncarlas");
        manager.CurrentPlan.Clear();for(int i=0;i<12;i++)manager.CurrentPlan.Add(new ActionBlock(ActionKind.Wait));board.Refresh();yield return null;
        var scroll=GameObject.Find("MainSequence").GetComponent<ScrollRect>();Check(scroll.content.rect.width>scroll.viewport.rect.width,"La secuencia larga se desplaza horizontalmente");
        manager.CurrentPlan.Clear();board.Refresh();
        var a=manager.PlayerA;var b=manager.PlayerB;
        a.activePhase=true;a.activeTrueRadar=true;a.queuedPhase=true;manager.PassTurn();Check(!a.activePhase&&!a.activeTrueRadar&&a.queuedPhase,"Pasar turno limpia poderes activos y conserva los pendientes");
        Check(manager.CurrentTurn==1&&Camera.allCameras.Length==1,"Cambio a B con una cámara");manager.PassTurn();Check(a.activePhase&&!a.queuedPhase,"Los poderes pendientes se activan al volver al turno propio");
        manager.EditTimeLimit=.1f;manager.PassTurn();manager.PlayerB.activePhase=true;yield return new WaitForSecondsRealtime(.18f);
        Check(!b.activePhase,"El vencimiento de tiempo elimina poderes activos");manager.EditTimeLimit=30;while(manager.CurrentTurn!=0)manager.PassTurn();
        // Reset the current timer after the short expiry test.
        manager.PassTurn();manager.PassTurn();
        var grid=Grid(Find<MazeGenerator>());
        var phaseCheck=CheckPhase(manager,board,grid);
        while(phaseCheck.MoveNext())yield return phaseCheck.Current;
        var historical=manager.GetLastKnownRadarFor(a).pos;
        var safe=new Vector2Int(2,2);if(Exit(grid,safe))safe=new Vector2Int(2,3);b.TeleportTo(safe);a.activeTrueRadar=true;
        Check(manager.GetLastKnownRadarFor(a).pos==historical&&a.activeTrueRadar,"Radar normal conserva el dato histórico y el poder");
        Check(manager.GetLastKnownRadarFor(a,true).pos==safe&&!a.activeTrueRadar,"Radar verdadero obtiene posición actual y se consume");
        a.activeTrueRadar=true;manager.CurrentPlan.Clear();manager.CurrentPlan.Add(new ActionBlock(ActionKind.Shoot){UseTrueRadar=true});board.Refresh();manager.Execute();
        Check(b.health.currentHealth==80,"Disparo acertado resta 20 de vida");a.ShootAt(b.CellPosition);Check(b.health.currentHealth==80,"Se bloquea un segundo disparo dentro del turno");
        yield return new WaitForSecondsRealtime(.15f);Check(manager.CurrentTurn==1,"La ejecución termina y cambia el turno");manager.PassTurn();
        manager.CurrentPlan.Clear();manager.CurrentPlan.Add(new ActionBlock(ActionKind.Wait));manager.CurrentPlan.Add(new ActionBlock(ActionKind.Shoot){UseTrueRadar=true});board.Refresh();
        ScreenCapture.CaptureScreenshot(Path.Combine(_output,"gameplay-panel.png"));yield return new WaitForEndOfFrame();yield return new WaitForSecondsRealtime(.15f);
        var icon=new ActionBlock(ActionKind.Repeat);icon.Children.Add(new ActionBlock(ActionKind.Up));icon.Children.Add(new ActionBlock(ActionKind.Right));manager.CurrentPlan.Add(icon);board.Refresh();board.OpenEditor(icon);yield return null;
        ScreenCapture.CaptureScreenshot(Path.Combine(_output,"editor-repeat.png"));yield return new WaitForEndOfFrame();yield return new WaitForSecondsRealtime(.15f);
        // Victory must still work by reaching the existing exit cell.
        Vector2Int exit=Vector2Int.zero;for(int x=0;x<6;x++)for(int y=0;y<6;y++)if(Exit(grid,new Vector2Int(x,y)))exit=new Vector2Int(x,y);
        a.TeleportTo(exit);Check(manager.GameEnded,"Llegar a la salida termina la partida");yield return null;
        Check(Find<GameOverManager>()!=null&&Time.timeScale==0,"Se muestra el resultado y se detiene el juego");
        Check(_errors.Count==0,"Sin errores de Unity durante la integración");
    }

    private IEnumerator CheckPhase(TurnBasedManager manager, MovementBoard board, MazeCell[,] grid)
    {
        var player = manager.CurrentPlayer;
        var phase = Array.Find(FindObjectsByType<Powerup>(FindObjectsSortMode.None), p => p.Type == Powerup.PowerupType.Phase);
        Check(phase != null, "El laberinto contiene un Phase real");
        Vector2Int pickup = new Vector2Int(Mathf.RoundToInt(phase.transform.position.x), Mathf.RoundToInt(phase.transform.position.z));
        var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        int approach = Array.FindIndex(directions, dir => Inside(grid, pickup - dir) &&
            !Exit(grid, pickup - dir) && !Wall(grid[pickup.x, pickup.y], -dir));
        Check(approach >= 0, "Hay un pasillo por donde llegar a Phase");
        player.TeleportTo(pickup - directions[approach]);
        Click("Palette_" + (ActionKind)approach);
        Click("ExecuteRoute");
        Check(player.CellPosition == pickup && phase.IsCollected && player.queuedPhase && !player.activePhase,
            "Un movimiento recoge Phase y lo guarda para el siguiente turno propio");
        while (manager.IsExecuting) yield return null;
        Click("PassTurn");
        Check(manager.CurrentPlayer == player && player.activePhase && !player.queuedPhase &&
            manager.Feedback.Contains("seleccioná un movimiento"), "Al volver al turno propio Phase se activa y explica cómo usarlo");

        Check(player.TryMove(-directions[approach], true) && player.activePhase, "Phase se conserva si la casilla está libre");
        for (int x = 0; x < grid.GetLength(0); x++)
        for (int y = 0; y < grid.GetLength(1); y++)
        for (int i = 0; i < directions.Length; i++)
        {
            var from = new Vector2Int(x, y);
            var to = from + directions[i];
            if (!Inside(grid, to) || Exit(grid, from) || Exit(grid, to) || !Wall(grid[x,y], directions[i]))
                continue;

            player.TeleportTo(from);
            manager.CurrentPlan.Clear();
            board.Refresh();
            var kind = (ActionKind)i;
            Click("Palette_" + kind);
            Click("Palette_" + kind);
            Click("Block_1_" + kind);
            Check(GameObject.Find("PhaseExplanation").GetComponentInChildren<Text>().text.Contains("Phase disponible este turno"),
                "El editor del movimiento indica la carga disponible");
            Click("PhaseToggle");
            Click("CloseEditor");
            Check(!manager.CurrentPlan[0].UsePhase && manager.CurrentPlan[1].UsePhase, "Phase queda marcado solo en el bloque elegido");
            float previousDelay = manager.StepDelay;
            manager.StepDelay = .15f;
            Click("ExecuteRoute");
            Check(player.CellPosition == from && player.activePhase, "El primer bloque sin Phase queda bloqueado y conserva la carga");
            while (manager.IsExecuting && manager.StepNumber < 2) yield return null;
            Check(player.CellPosition == to && !player.activePhase && manager.Feedback.Contains("atravesaste un muro"),
                "El bloque elegido cruza el muro y consume Phase");
            Check(player.GetComponent<Rigidbody>().position == new Vector3(to.x, .5f, to.y),
                "El personaje se desplaza a la casilla al otro lado del muro");
            while (manager.IsExecuting) yield return null;
            manager.StepDelay = previousDelay;
            Click("PassTurn");
            yield break;
        }
        Check(false, "Existe un muro interior para probar Phase");
    }

    private void CheckInstructions()
    {
        var text=Array.Find(FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None),
            label=>label.text.Contains(ActionPlan.FirstTurnShotError.TrimEnd('.')));
        Check(text!=null&&text.text.Contains("Secuencia")&&!text.text.Contains("Shoot("),"Instrucciones actuales visibles");
        text.ForceMeshUpdate();
        Check(!text.isTextOverflowing,"El texto de instrucciones cabe en el panel");
    }

    private void CheckFirstTurnShotBlocked(TurnBasedManager manager, MovementBoard board, bool nested)
    {
        var palette=GameObject.Find("Palette_Shoot");
        Check(!manager.CanShootThisTurn&&!palette.GetComponent<Button>().interactable,"Disparar está deshabilitado en el primer turno de "+manager.CurrentTurn);
        board.Add(ActionKind.Shoot);
        var drag=palette.GetComponent<ActionDrag>();
        var zone=GameObject.Find("MainSequence").GetComponent<ScrollRect>().viewport;
        var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,zone.TransformPoint(zone.rect.center))};
        board.BeginDrag(drag,pointer);
        board.EndDrag(pointer);
        Check(manager.CurrentPlan.Count==0,"Ni el clic ni el arrastre agregan un disparo en el primer turno");

        var shot=new ActionBlock(ActionKind.Shoot){UseTrueRadar=true};
        var repeat=new ActionBlock(ActionKind.Repeat){Repetitions=1};
        repeat.Children.Add(shot);
        manager.CurrentPlan.Add(nested?repeat:shot);
        board.Refresh();
        manager.Execute();
        Check(!manager.IsExecuting&&manager.Feedback==ActionPlan.FirstTurnShotError,"El turno rechaza el disparo "+(nested?"dentro de Repetir":"directo"));
        Check(manager.PlayerA.health.currentHealth==100&&manager.PlayerB.health.currentHealth==100,"El disparo rechazado no resta vida");
    }

    private void Finish(string failure)
    {
        if(_finished)return;_finished=true;
        string result=(failure==null?"PASS":"FAIL")+"\n"+string.Join("\n",_checks)+"\n"+(failure??"")+"\n"+string.Join("\n",_errors);
        File.WriteAllText(Path.Combine(_output,"resultado.txt"),result);Debug.Log("MAZE_SMOKE_"+(failure==null?"PASS":"FAIL"));Application.Quit(failure==null?0:1);
    }
}
#endif

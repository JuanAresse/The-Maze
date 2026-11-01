#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Opt-in regression checks. Excluded from release builds.
public class MazeRegressionTest : MonoBehaviour
{
    private string _output;
    private readonly List<string> _results = new List<string>();
    private readonly List<string> _errors = new List<string>();
    private int _failures;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        var args = Environment.GetCommandLineArgs();
        if(Array.IndexOf(args,"-maze-regression-test")<0)return;
        var runner = new GameObject("RegressionChecks").AddComponent<MazeRegressionTest>();
        DontDestroyOnLoad(runner);
        int index=Array.IndexOf(args,"-maze-regression-output");
        runner._output=index>=0?args[index+1]:Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"verificacion_regresiones");
        Directory.CreateDirectory(runner._output);
        Application.runInBackground=true;
        Application.logMessageReceived+=(message,trace,type)=>{if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)runner._errors.Add(message+"\n"+trace);};
        runner.StartCoroutine(runner.Guard());
    }
    private IEnumerator Guard()
    {
        var run=Run();
        while(true)
        {
            bool next=false;object value=null;
            try{next=run.MoveNext();if(next)value=run.Current;}
            catch(Exception error){_failures++;_results.Add("HARNESS ERROR: "+error);break;}
            if(!next)break;
            yield return value;
        }
        _results.Add("Unity runtime errors: "+_errors.Count);
        _results.AddRange(_errors);
        bool passed=_failures==0&&_errors.Count==0;
        _results.Insert(0,passed?"PASS":"FAIL");
        File.WriteAllLines(Path.Combine(_output,"resultado.txt"),_results);
        Debug.Log(passed?"MAZE_REGRESSION_PASS":"MAZE_REGRESSION_FAIL");
        Application.Quit(passed?0:1);
    }
    private T Find<T>() where T:UnityEngine.Object=>FindFirstObjectByType<T>();
    private object Field(object owner,string name)=>owner.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(owner);
    private void Result(bool good,string name,string details="")
    {
        if(!good)_failures++;
        _results.Add((good?"PASS: ":"FAIL: ")+name+" | "+details);
    }
    private IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(_output,name+".png"));
        yield return new WaitForEndOfFrame();
        yield return new WaitForSecondsRealtime(.1f);
    }
    private IEnumerator Run()
    {
        while(!UnityEngine.Rendering.SplashScreen.isFinished)yield return null;
        Find<ControlMenu>().Play();yield return null;
        GameObject.Find("Size_6").GetComponent<Button>().onClick.Invoke();
        GameObject.Find("StartMatch").GetComponent<Button>().onClick.Invoke();
        yield return new WaitForSecondsRealtime(.8f);
        var manager=Find<TurnBasedManager>();var board=Find<MovementBoard>();var menu=Find<ControlMenu>();
        manager.EditTimeLimit=300;manager.PassTurn();manager.PassTurn();

        // Feed a virtual mouse through the game's actual Input System UI module.
        // This exercises raycasts and Begin/Drag/End dispatch, not Board methods.
        var inputModule=EventSystem.current.currentInputModule;
        _results.Add("INFO: UI input module: "+inputModule.GetType().Name);
        var mouse=InputSystem.AddDevice<Mouse>("AuditMouse");
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        var paletteRect=GameObject.Find("Palette_Up").GetComponent<RectTransform>();
        var routeRect=GameObject.Find("MainSequence").GetComponent<ScrollRect>().viewport;
        var fromPoint=RectTransformUtility.WorldToScreenPoint(null,paletteRect.TransformPoint(paletteRect.rect.center));
        var toPoint=RectTransformUtility.WorldToScreenPoint(null,routeRect.TransformPoint(routeRect.rect.center));
        InputSystem.QueueStateEvent(mouse,new MouseState{position=fromPoint});yield return new WaitForSecondsRealtime(.05f);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=fromPoint}.WithButton(MouseButton.Left));yield return new WaitForSecondsRealtime(.05f);
        for(int i=1;i<=10;i++)
        {
            InputSystem.QueueStateEvent(mouse,new MouseState{position=Vector2.Lerp(fromPoint,toPoint,i/10f)}.WithButton(MouseButton.Left));
            yield return new WaitForSecondsRealtime(.03f);
        }
        InputSystem.QueueStateEvent(mouse,new MouseState{position=toPoint});yield return new WaitForSecondsRealtime(.1f);
        Result(manager.CurrentPlan.Count==1&&manager.CurrentPlan[0].Kind==ActionKind.Up,"Palette drag through real input module",$"routeCount={manager.CurrentPlan.Count}; from={fromPoint}; to={toPoint}; ghostAlive={GameObject.Find("DraggingAction")!=null}");
        yield return Capture("arrastre-modulo-real");
        InputSystem.RemoveDevice(mouse);
        manager.CurrentPlan.Clear();board.SetPlan(manager.CurrentPlan);

        menu.Pause();menu.OpenOptionsFromPause();menu.TogglePause();yield return null;
        bool options=((GameObject)Field(menu,"optionsPanel")).activeInHierarchy;
        float time=manager.TimeRemaining;yield return new WaitForSecondsRealtime(.3f);
        Result(!options||Time.timeScale==0,"Escape from pause options keeps gameplay paused or closes options",$"optionsVisible={options}; timeScale={Time.timeScale}; timer {time:F2}->{manager.TimeRemaining:F2}");
        Result(Time.timeScale==0&&Mathf.Abs(manager.TimeRemaining-time)<.01f&&((GameObject)Field(menu,"pausePanel")).activeInHierarchy,"Escape from options returns to pause and freezes timer");
        yield return Capture("bug-opciones-escape");
        menu.BackToPause();menu.Resume();
        menu.Pause();menu.OpenInstructionsFromPause();menu.TogglePause();yield return null;
        bool instructions=((GameObject)Field(menu,"instructionsPanel")).activeInHierarchy;
        Result(!instructions||Time.timeScale==0,"Escape from pause instructions keeps gameplay paused or closes instructions",$"instructionsVisible={instructions}; timeScale={Time.timeScale}");
        menu.BackToPause();menu.Resume();

        // Validation is cached between edits; edits still update the action budget.
        manager.CurrentPlan.Add(new ActionBlock(ActionKind.Wait));board.Refresh();
        var validation=Field(board,"_validation");
        yield return new WaitForSecondsRealtime(.1f);
        Result(ReferenceEquals(validation,Field(board,"_validation")),"Validation is reused while the plan is unchanged");
        board.Add(ActionKind.Wait);
        Result(((PlanValidation)Field(board,"_validation")).Actions==2,"Adding an action updates cached budget");
        GameObject.Find("Block_1_Wait").transform.Find("Remove").GetComponent<Button>().onClick.Invoke();
        Result(((PlanValidation)Field(board,"_validation")).Actions==1,"Removing an action updates cached budget");

        manager.CurrentPlan.Clear();
        var repeat=new ActionBlock(ActionKind.Repeat){Repetitions=1};repeat.Children.Add(new ActionBlock(ActionKind.Wait));
        var rootWait=new ActionBlock(ActionKind.Wait);manager.CurrentPlan.Add(repeat);manager.CurrentPlan.Add(rootWait);
        board.Refresh();board.OpenEditor(repeat);manager.StepDelay=.5f;manager.Execute();
        yield return new WaitForSecondsRealtime(.6f);
        Result(manager.ActiveStep!=null&&manager.ActiveStep.Block==rootWait&&GameObject.Find("ActionEditor")==null,"Repeat editor closes before next root action",$"rootStepActive={manager.ActiveStep!=null&&manager.ActiveStep.Block==rootWait}; popupVisible={GameObject.Find("ActionEditor")!=null}; selectedIsNull={Field(board,"_selected")==null}");
        yield return Capture("editor-repeticion-corregido");
        yield return new WaitForSecondsRealtime(.7f);manager.PassTurn();

        manager.CurrentPlan.Clear();
        var hiddenRepeat=new ActionBlock(ActionKind.Repeat);hiddenRepeat.Children.Add(new ActionBlock(ActionKind.Wait));
        for(int i=0;i<12;i++)manager.CurrentPlan.Add(i==2?hiddenRepeat:new ActionBlock(ActionKind.Wait));
        board.SetPlan(manager.CurrentPlan);yield return null;
        var main=GameObject.Find("MainSequence").GetComponent<ScrollRect>();main.horizontalNormalizedPosition=1;Canvas.ForceUpdateCanvases();yield return null;
        var tile=GameObject.Find("Block_2_Repeat").GetComponent<RectTransform>();
        var point=RectTransformUtility.WorldToScreenPoint(null,tile.TransformPoint(tile.rect.center));
        bool clipped=!RectTransformUtility.RectangleContainsScreenPoint(main.viewport,point,null);
        var pointer=new PointerEventData(EventSystem.current){position=point};
        board.BeginDrag(GameObject.Find("Palette_Up").GetComponent<ActionDrag>(),pointer);
        yield return new WaitForSecondsRealtime(.85f);
        bool opened=Field(board,"_selected")==hiddenRepeat;
        Result(!clipped||!opened,"Invisible clipped repeat does not react to drag hover",$"point={point}; outsideViewport={clipped}; invisibleRepeatOpened={opened}");
        yield return Capture("hover-oculto-corregido");board.SetPlan(manager.CurrentPlan);

        // An actual visible repeat must still open when hovered.
        main.horizontalNormalizedPosition=0;Canvas.ForceUpdateCanvases();yield return null;
        tile=GameObject.Find("Block_2_Repeat").GetComponent<RectTransform>();
        point=RectTransformUtility.WorldToScreenPoint(null,tile.TransformPoint(tile.rect.center));
        pointer=new PointerEventData(EventSystem.current){position=point};
        board.BeginDrag(GameObject.Find("Palette_Up").GetComponent<ActionDrag>(),pointer);
        yield return new WaitForSecondsRealtime(.85f);
        Result(Field(board,"_selected")==hiddenRepeat,"Visible repeat still opens on drag hover");
        board.SetPlan(manager.CurrentPlan);

        // Measure the real menu rects at three resolutions; no claim based on screenshots alone.
        foreach(var resolution in new[]{new Vector2Int(1280,720),new Vector2Int(1024,576),new Vector2Int(1920,1080)})
        {
            Screen.SetResolution(resolution.x,resolution.y,false);yield return new WaitForSecondsRealtime(.3f);
            menu.Pause();yield return null;Canvas.ForceUpdateCanvases();
            var pause=(GameObject)Field(menu,"pausePanel");
            foreach(var button in pause.GetComponentsInChildren<Button>())
            {
                var corners=new Vector3[4];button.GetComponent<RectTransform>().GetWorldCorners(corners);
                bool visible=true;foreach(var p in corners){var s=RectTransformUtility.WorldToScreenPoint(null,p);visible&=s.x>=0&&s.y>=0&&s.x<=Screen.width&&s.y<=Screen.height;}
                Result(visible,"Pause button fully visible: "+button.name,$"resolution={Screen.width}x{Screen.height}; lowerLeft={corners[0]}; upperRight={corners[2]}");
            }
            if(resolution.x==1024)yield return Capture("pausa-1024-corregida");
            menu.Resume();
        }

        // Multiple random real scene generations, reciprocal walls and actual winning routes.
        int maps=0,wins=0;bool connected=true,reciprocal=true,cameras=true;
        foreach(int size in new[]{4,6,8})for(int seed=0;seed<8;seed++)
        {
            UnityEngine.Random.InitState(size*100+seed);GameOptions.ChooseSize(size);SceneManager.LoadScene("The maze");
            yield return new WaitForSecondsRealtime(.12f);
            manager=Find<TurnBasedManager>();manager.EditTimeLimit=300;manager.StepDelay=.001f;
            var grid=(MazeCell[,])Field(Find<MazeGenerator>(),"_mazeGrid");
            var start=manager.PlayerA.CellPosition;var parents=new Dictionary<Vector2Int,Vector2Int>();var dirs=new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
            var queue=new Queue<Vector2Int>();queue.Enqueue(start);parents[start]=start;Vector2Int exit=new Vector2Int(-1,-1);
            while(queue.Count>0)
            {
                var p=queue.Dequeue();if(grid[p.x,p.y].GetComponentInChildren<ExitZone>()!=null)exit=p;
                foreach(var dir in dirs)
                {
                    var n=p+dir;if(n.x<0||n.y<0||n.x>=size||n.y>=size)continue;
                    bool wall=Wall(grid[p.x,p.y],dir);reciprocal&=wall==Wall(grid[n.x,n.y],-dir);
                    if(!wall&&!parents.ContainsKey(n)){parents[n]=p;queue.Enqueue(n);}
                }
            }
            maps++;connected&=parents.Count==size*size;cameras&=Camera.allCameras.Length==1;
            var path=new List<Vector2Int>();for(var p=exit;p!=start;p=parents[p])path.Insert(0,p-parents[p]);
            for(int i=0;i<path.Count&&!manager.GameEnded;)
            {
                manager.CurrentPlan.Clear();for(int n=0;n<8&&i<path.Count;n++,i++){var d=path[i];manager.CurrentPlan.Add(new ActionBlock(d==Vector2Int.up?ActionKind.Up:d==Vector2Int.down?ActionKind.Down:d==Vector2Int.left?ActionKind.Left:ActionKind.Right));}
                manager.Execute();while(manager.IsExecuting)yield return null;
                if(!manager.GameEnded)manager.PassTurn();
            }
            if(manager.GameEnded&&manager.PlayerA.CellPosition==exit)wins++;
        }
        Result(connected&&reciprocal&&cameras&&wins==maps,"24 real maps and movement routes to victory",$"maps={maps}; wins={wins}; connected={connected}; reciprocalWalls={reciprocal}; singleCamera={cameras}");

        var gameOver=Find<GameOverManager>();
        Result(gameOver!=null&&gameOver.gameOverUI.activeInHierarchy,"Victory screen remains visible");
        gameOver.mainMenuButton.onClick.Invoke();yield return new WaitForSecondsRealtime(.25f);
        Result(SceneManager.GetActiveScene().name=="MENU"&&Time.timeScale==1,"Result screen returns to menu and resumes time");
    }
    private bool Wall(MazeCell cell,Vector2Int d)=>d==Vector2Int.up?cell.HasFrontWall():d==Vector2Int.down?cell.HasBackWall():d==Vector2Int.left?cell.HasLeftWall():cell.HasRightWall();
}
#endif

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MovementBoard : MonoBehaviour
{
    private TurnBasedManager _manager;
    private Canvas _canvas;
    private CanvasGroup _group;
    private RectTransform _tray;
    private RectTransform _popup;
    private ScrollRect _mainScroll;
    private readonly List<ActionDropZone> _zones = new List<ActionDropZone>();
    private readonly Dictionary<ActionBlock, List<Image>> _tiles = new Dictionary<ActionBlock, List<Image>>();
    private Text _title, _status, _feedback, _budget;
    private Button _execute, _continuous, _shoot;
    private Text _continuousLabel;
    private PlanValidation _validation;
    private List<ActionBlock> _plan;
    private ActionBlock _selected;
    private bool _lastEdit;
    private bool _lastPaused;
    private RectTransform _ghost, _marker;
    private ActionBlock _dragBlock;
    private List<ActionBlock> _dragSource;
    private ActionKind _dragKind;
    private Vector2 _pointer;
    private ActionDropZone _dropZone;
    private int _insertion;
    private float _hoverTime;
    private ActionBlock _hoverRepeat;

    public void Initialize(TurnBasedManager manager)
    {
        _manager = manager;
        _canvas = MazeUI.Canvas("MovementControls", 10);
        _group = _canvas.gameObject.AddComponent<CanvasGroup>();
        _tray = MazeUI.Box("MovementTray", _canvas.transform, Vector2.zero, new Vector2(1,0), MazeUI.Panel);
        _tray.pivot = new Vector2(.5f,0); _tray.sizeDelta = new Vector2(0,208);
        _title = LabelAt(_tray,"ActivePlayer","",new Vector2(.012f,.82f),new Vector2(.40f,.99f),20);
        _status = LabelAt(_tray,"Status","",new Vector2(.40f,.82f),new Vector2(.99f,.99f),15,TextAnchor.MiddleRight);
        BuildPalette();
        _budget = LabelAt(_tray,"Budget","",new Vector2(.32f,.69f),new Vector2(.85f,.82f),14);
        MazeUI.Button("ClearRoute",_tray,"Vaciar",new Vector2(.89f,.69f),new Vector2(.96f,.82f),()=>
        {
            if(!CanEdit)return;
            CurrentList().Clear();
            if(_selected!=null&&_selected.Kind!=ActionKind.Repeat){_selected=null;ClosePopup();}
            Changed();
        });
        _mainScroll = BuildScroll(_tray,"MainSequence",new Vector2(.32f,.24f),new Vector2(.96f,.69f),null);
        MazeUI.Button("ScrollLeft",_tray,"<",new Vector2(.295f,.35f),new Vector2(.32f,.58f),()=>ScrollBy(_mainScroll,-180));
        MazeUI.Button("ScrollRight",_tray,">",new Vector2(.963f,.35f),new Vector2(.99f,.58f),()=>ScrollBy(_mainScroll,180));
        _feedback = LabelAt(_tray,"Feedback","",new Vector2(.012f,.01f),new Vector2(.59f,.22f),14);
        MazeUI.Button("PassTurn",_tray,"Pasar turno",new Vector2(.60f,.025f),new Vector2(.71f,.21f),()=>_manager.PassTurn());
        _continuous = MazeUI.Button("Continuous",_tray,"Continuo",new Vector2(.71f,.025f),new Vector2(.82f,.21f),()=>_manager.ToggleContinuous());
        _continuousLabel = _continuous.GetComponentInChildren<Text>();
        _execute = MazeUI.Button("ExecuteRoute",_tray,"Finalizar y ejecutar",new Vector2(.82f,.025f),new Vector2(.99f,.21f),()=>_manager.Execute(),MazeUI.Green);
    }

    public bool CanEdit => _manager != null && _manager.CanEdit;
    private List<ActionBlock> CurrentList() => _selected != null && _selected.Kind == ActionKind.Repeat ? _selected.Children : _plan;

    private void BuildPalette()
    {
        for (int i=0;i<7;i++)
        {
            var kind=(ActionKind)i; float x=.012f+(i%4)*.070f;float y=i<4?.535f:.285f;
            var button=MazeUI.Button("Palette_"+kind,_tray,"",new Vector2(x,y),new Vector2(x+.068f,y+.245f),()=>Add(kind));
            if(kind==ActionKind.Shoot)_shoot=button;
            AddIcon(button.transform,kind,new Vector2(.34f,.36f),new Vector2(.66f,.97f),MazeUI.Ink);
            LabelAt(button.transform,"Name",ActionPlan.Label(kind),new Vector2(0,0),new Vector2(1,.34f),12,TextAnchor.MiddleCenter).color=MazeUI.Ink;
            var drag=button.gameObject.AddComponent<ActionDrag>();drag.Board=this;drag.Kind=kind;
        }
    }

    public void SetPlan(List<ActionBlock> plan)
    {
        CancelDrag();_plan=plan;_selected=null;ClosePopup();Refresh();
        _mainScroll.horizontalNormalizedPosition=0;
    }

    public void Refresh()
    {
        if (_plan==null)return;
        _validation=_manager.ValidateCurrentPlan();
        float position=_mainScroll.horizontalNormalizedPosition;
        _tiles.Clear(); _zones.Clear();
        FillScroll(_mainScroll,_plan,null);
        _mainScroll.horizontalNormalizedPosition=position;
        if(_selected!=null)BuildEditor();
        else ClosePopup();
        if(_manager.ActiveStep!=null)
        {
            foreach(var parent in _manager.ActiveStep.Parents)ColorTile(parent);
            ColorTile(_manager.ActiveStep.Block);
        }
        ApplyLocks();UpdateStatus();
    }

    public void UpdateStatus()
    {
        if(_manager==null||_title==null||_plan==null)return;
        _title.text=(_manager.CurrentTurn==0?"Jugador A":"Jugador B")+(_manager.IsExecuting?" · Ejecutando":" · Armá tu ruta");
        var player=_manager.CurrentPlayer;
        string phase=player.activePhase?"disponible":player.queuedPhase?"próximo turno":"—";
        string radar=player.activeTrueRadar?"disponible":player.queuedTrueRadar?"próximo turno":"—";
        _status.text=(_manager.IsExecuting?$"Paso {_manager.StepNumber}/{_manager.StepCount}":$"Tiempo: {Mathf.CeilToInt(_manager.TimeRemaining)} s")+$"  |  Vida: {player.health.currentHealth}/100  |  Phase: {phase}  |  Radar: {radar}";
        var check=_validation;
        if(check==null)return;
        _budget.text=_manager.CanShootThisTurn
            ? $"SECUENCIA · {check.Actions}/8 acciones · {check.Shots}/1 disparos"
            : $"SECUENCIA · {check.Actions}/8 acciones · Disparo desde el 2.º turno";
        _budget.color=check.IsValid||_plan.Count==0?MazeUI.Muted:new Color(1,.70f,.35f);
        _feedback.text=_manager.Feedback;
        if(!check.IsValid&&_plan.Count>0&&!_manager.IsExecuting)_feedback.text=check.Error;
        _execute.interactable=CanEdit&&check.IsValid;
        _shoot.interactable=CanEdit&&_manager.CanShootThisTurn;
        _continuousLabel.text=_manager.Continuous?"Detener":"Continuo";
        _continuous.interactable=!_manager.GameEnded&&Time.timeScale>0;
    }

    private void Update()
    {
        if(_manager==null)return;
        bool paused=Time.timeScale<=0f;
        _group.alpha=paused?0:1;_group.blocksRaycasts=!paused&&!_manager.GameEnded;
        if(paused&&!_lastPaused)CancelDrag();
        if(_lastEdit!=CanEdit||paused!=_lastPaused){ApplyLocks();_lastEdit=CanEdit;_lastPaused=paused;}
        UpdateStatus();
        if(_ghost!=null&&CanEdit)
        {
            DetectDrop(_pointer);
            if(_dropZone!=null)
            {
                RectTransform viewport=_dropZone.transform as RectTransform;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport,_pointer,null,out var point);
                float edge=36;
                if(point.x<viewport.rect.xMin+edge)ScrollBy(_dropZone.Scroll,-380*Time.unscaledDeltaTime);
                if(point.x>viewport.rect.xMax-edge)ScrollBy(_dropZone.Scroll,380*Time.unscaledDeltaTime);
            }
            // Holding over a collapsed repeat opens its inner drop area.
            ActionBlock hovered=null;
            foreach(var zone in _zones)
            {
                if(!RectTransformUtility.RectangleContainsScreenPoint(zone.Scroll.viewport,_pointer,null))continue;
                for(int i=0;i<zone.Tiles.Count;i++)
                {
                    var block=zone.Items[i];
                    if(block.Kind==ActionKind.Repeat&&block!=_dragBlock&&
                        RectTransformUtility.RectangleContainsScreenPoint(zone.Tiles[i],_pointer,null))hovered=block;
                }
            }
            if(hovered!=_hoverRepeat){_hoverRepeat=hovered;_hoverTime=0;}
            if(hovered!=null&&hovered!=_selected&&(_dragBlock==null||!ActionPlan.Contains(_dragBlock,hovered)))
            {
                _hoverTime+=Time.unscaledDeltaTime;
                if(_hoverTime>.65f){OpenEditor(hovered);_hoverTime=0;}
            }
        }
    }

    private void ApplyLocks()
    {
        if(_canvas==null)return;
        foreach(var button in _canvas.GetComponentsInChildren<Button>(true))button.interactable=CanEdit;
        _lastEdit=CanEdit;UpdateStatus();
    }

    public void Add(ActionKind kind)
    {
        if(!CanEdit)return;
        if(kind==ActionKind.Shoot&&!_manager.CanShootThisTurn)return;
        if(StoredCount(_plan)>=ActionPlan.MaxBlocks){_manager.ShowFeedback("El editor admite hasta 64 bloques. Reducí la ruta a 8 acciones para ejecutar.");return;}
        var block=new ActionBlock(kind);CurrentList().Add(block);Changed();
        if(kind==ActionKind.Repeat)OpenEditor(block);
        else if(_selected!=null&&_selected.Kind==ActionKind.Repeat)ScrollToEnd(_zones[_zones.Count-1].Scroll);
        else ScrollToEnd(_mainScroll);
    }

    private int StoredCount(List<ActionBlock> list)
    {int n=0;foreach(var b in list)n+=1+StoredCount(b.Children);return n;}

    private void Changed()
    {
        _manager.ShowFeedback("Ruta actualizada. Máximo: 8 acciones ejecutadas y un disparo.");Refresh();
    }

    private ScrollRect BuildScroll(Transform parent,string name,Vector2 min,Vector2 max,ActionBlock owner)
    {
        var root=MazeUI.Rect(name,parent,min,max);
        var scroll=root.gameObject.AddComponent<ScrollRect>();scroll.horizontal=true;scroll.vertical=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.inertia=false;scroll.scrollSensitivity=32;
        var viewport=MazeUI.Box("Viewport",root,new Vector2(0,.15f),Vector2.one,new Color(.18f,.20f,.22f));
        viewport.gameObject.AddComponent<RectMask2D>();scroll.viewport=viewport;
        var content=MazeUI.Rect("Content",viewport,Vector2.zero,new Vector2(0,1));content.pivot=new Vector2(0,.5f);
        var layout=content.gameObject.AddComponent<HorizontalLayoutGroup>();layout.spacing=8;layout.padding=new RectOffset(7,7,5,5);layout.childControlWidth=false;layout.childControlHeight=true;layout.childForceExpandWidth=false;layout.childForceExpandHeight=true;
        content.gameObject.AddComponent<ContentSizeFitter>().horizontalFit=ContentSizeFitter.FitMode.PreferredSize;
        scroll.content=content;
        var barRoot=MazeUI.Box("Scrollbar",root,Vector2.zero,new Vector2(1,.10f),new Color(.28f,.30f,.32f));
        var bar=barRoot.gameObject.AddComponent<Scrollbar>();var handle=MazeUI.Box("Handle",barRoot,Vector2.zero,Vector2.one,MazeUI.Muted);
        bar.handleRect=handle;bar.targetGraphic=handle.GetComponent<Image>();bar.direction=Scrollbar.Direction.LeftToRight;scroll.horizontalScrollbar=bar;
        var zone=viewport.gameObject.AddComponent<ActionDropZone>();zone.Scroll=scroll;zone.Owner=owner;
        return scroll;
    }

    private void FillScroll(ScrollRect scroll,List<ActionBlock> items,ActionBlock owner)
    {
        foreach(Transform child in scroll.content){child.gameObject.SetActive(false);Destroy(child.gameObject);}
        var zone=scroll.viewport.GetComponent<ActionDropZone>();zone.Items=items;zone.Owner=owner;zone.Tiles.Clear();_zones.Add(zone);
        for(int i=0;i<items.Count;i++)
        {
            var block=items[i];var button=MazeUI.Button("Block_"+i+"_"+block.Kind,scroll.content,"",Vector2.zero,Vector2.one,()=>OpenEditor(block));
            var rect=button.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,.5f);rect.sizeDelta=new Vector2(block.Kind==ActionKind.Repeat?150:106,0);
            button.gameObject.AddComponent<LayoutElement>().preferredWidth=rect.sizeDelta.x;
            zone.Tiles.Add(rect);
            AddIcon(rect,block.Kind,new Vector2(.32f,.34f),new Vector2(.68f,.82f),MazeUI.Ink);
            string caption=ActionPlan.Label(block.Kind);
            if(block.Kind==ActionKind.Repeat)caption=$"{block.Repetitions} veces · {block.Children.Count} bloques";
            else if(block.UsePhase)caption+=" · Phase";
            else if(block.Kind==ActionKind.Shoot&&block.UseTrueRadar)caption="Radar verdadero";
            LabelAt(rect,"Caption",caption,new Vector2(.02f,.01f),new Vector2(.98f,.29f),12,TextAnchor.MiddleCenter).color=MazeUI.Ink;
            LabelAt(rect,"Order",(i+1).ToString(),new Vector2(.02f,.68f),new Vector2(.20f,.98f),11).color=MazeUI.Ink;
            var remove=MazeUI.Button("Remove",rect,"×",new Vector2(.75f,.60f),new Vector2(1,1),()=>{if(CanEdit){items.Remove(block);if(_selected!=null&&ActionPlan.Contains(block,_selected)){_selected=null;ClosePopup();}Changed();}});
            remove.GetComponentInChildren<Text>().fontSize=14;
            var drag=rect.gameObject.AddComponent<ActionDrag>();drag.Board=this;drag.Block=block;drag.Source=items;
            if(!_tiles.ContainsKey(block))_tiles[block]=new List<Image>();_tiles[block].Add(rect.GetComponent<Image>());
        }
        if(items.Count==0)
        {
            var hint=MazeUI.Rect("EmptyHint",scroll.content,Vector2.zero,new Vector2(0,1));hint.sizeDelta=new Vector2(250,0);hint.gameObject.AddComponent<LayoutElement>().preferredWidth=250;
            MazeUI.Text("Text",hint,"Soltá acciones aquí",14,TextAnchor.MiddleCenter).color=MazeUI.Muted;
        }
        Canvas.ForceUpdateCanvases();LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
    }

    public void OpenEditor(ActionBlock block)
    {
        if(!CanEdit)return;_selected=block;Refresh();
    }

    private void BuildEditor()
    {
        ClosePopup();
        _popup=MazeUI.Box("ActionEditor",_canvas.transform,Vector2.zero,new Vector2(1,0),MazeUI.Panel,new Vector2(12,216),new Vector2(-12,426));
        var block=_selected;
        LabelAt(_popup,"EditorTitle",ActionPlan.Label(block.Kind)+" · Configuración",new Vector2(.015f,.80f),new Vector2(.65f,.98f),19);
        MazeUI.Button("CloseEditor",_popup,"Listo",new Vector2(.90f,.80f),new Vector2(.985f,.98f),()=>{_selected=null;ClosePopup();Refresh();});
        var parent=ParentOf(_plan,block);
        if(parent!=null)MazeUI.Button("ParentEditor",_popup,"< Repetición",new Vector2(.74f,.80f),new Vector2(.90f,.98f),()=>OpenEditor(parent));
        if(block.Kind==ActionKind.Repeat)
        {
            LabelAt(_popup,"Count","Repetir "+block.Repetitions+" veces",new Vector2(.015f,.61f),new Vector2(.25f,.79f),17);
            MazeUI.Button("Less",_popup,"−",new Vector2(.25f,.61f),new Vector2(.30f,.79f),()=>{if(CanEdit){block.Repetitions=Mathf.Max(1,block.Repetitions-1);Changed();}});
            MazeUI.Button("More",_popup,"+",new Vector2(.30f,.61f),new Vector2(.35f,.79f),()=>{if(CanEdit){block.Repetitions=Mathf.Min(8,block.Repetitions+1);Changed();}});
            LabelAt(_popup,"RepeatHint","Arrastrá aquí o usá la paleta para añadir al bloque. Se cuentan las acciones repetidas.",new Vector2(.37f,.61f),new Vector2(.98f,.79f),14);
            var scroll=BuildScroll(_popup,"RepeatSequence",new Vector2(.015f,.05f),new Vector2(.985f,.59f),block);FillScroll(scroll,block.Children,block);
        }
        else if(block.IsMovement)
        {
            LabelAt(_popup,"PhaseExplanation","Phase permite atravesar un muro en ESTE movimiento. Se consume solo si hay un muro y el poder está disponible.",new Vector2(.02f,.38f),new Vector2(.98f,.77f),17);
            MazeUI.Button("PhaseToggle",_popup,block.UsePhase?"Phase en esta acción: SÍ":"Phase en esta acción: NO",new Vector2(.02f,.09f),new Vector2(.40f,.35f),()=>{if(CanEdit){block.UsePhase=!block.UsePhase;Changed();}},block.UsePhase?(Color?)MazeUI.Green:null);
        }
        else if(block.Kind==ActionKind.Shoot)
        {
            LabelAt(_popup,"RadarExplanation","Radar normal: posición y orientación anteriores al último turno ejecutado del rival. Los ajustes son relativos a esa orientación.",new Vector2(.015f,.61f),new Vector2(.70f,.79f),13);
            MazeUI.Button("TrueRadarToggle",_popup,block.UseTrueRadar?"Radar verdadero: SÍ":"Radar verdadero: NO",new Vector2(.71f,.61f),new Vector2(.985f,.79f),()=>{if(CanEdit){block.UseTrueRadar=!block.UseTrueRadar;Changed();}},block.UseTrueRadar?(Color?)MazeUI.Green:null);
            for(int i=0;i<6;i++)
            {
                var adjustment=(RadarAdjustment)i;float x=.015f+i*.162f;
                MazeUI.Button("Radar_"+adjustment,_popup,ActionPlan.AdjustmentLabel(adjustment),new Vector2(x,.35f),new Vector2(x+.16f,.60f),()=>{if(CanEdit&&block.Adjustments.Count<32){block.Adjustments.Add(adjustment);Changed();}}).GetComponentInChildren<Text>().fontSize=13;
            }
            var scroll=BuildScroll(_popup,"RadarAdjustments",new Vector2(.015f,.03f),new Vector2(.985f,.34f),null);
            // Adjustment chips are configuration, not executable actions or drop targets.
            scroll.viewport.GetComponent<ActionDropZone>().enabled=false;
            for(int i=0;i<block.Adjustments.Count;i++)
            {
                int index=i;var chip=MazeUI.Button("Adjustment_"+i,scroll.content,ActionPlan.AdjustmentLabel(block.Adjustments[i])+" ×",Vector2.zero,Vector2.one,()=>{if(CanEdit){block.Adjustments.RemoveAt(index);Changed();}});
                chip.GetComponent<RectTransform>().sizeDelta=new Vector2(190,0);chip.gameObject.AddComponent<LayoutElement>().preferredWidth=190;chip.GetComponentInChildren<Text>().fontSize=12;
            }
        }
        else LabelAt(_popup,"WaitExplanation","Esperar consume una de las 8 acciones y mantiene al personaje quieto durante un paso.",new Vector2(.02f,.10f),new Vector2(.98f,.75f),18);
    }

    private ActionBlock ParentOf(List<ActionBlock> list,ActionBlock child)
    {
        foreach(var b in list){if(b.Children.Contains(child))return b;var parent=ParentOf(b.Children,child);if(parent!=null)return parent;}return null;
    }

    private void ClosePopup()
    {
        if(_popup==null)return;_popup.gameObject.SetActive(false);Destroy(_popup.gameObject);_popup=null;
    }

    public void Highlight(PlanStep step)
    {
        _selected=step.Parents.Count>0?step.Parents[step.Parents.Count-1]:null;
        Refresh();
        foreach(var parent in step.Parents)ColorTile(parent);
        ColorTile(step.Block);
        BringIntoView(_mainScroll,step.Parents.Count>0?step.Parents[0]:step.Block);
        if(_zones.Count>1)BringIntoView(_zones[_zones.Count-1].Scroll,step.Block);
    }

    private void ColorTile(ActionBlock block)
    {if(_tiles.TryGetValue(block,out var images))foreach(var image in images)if(image!=null)image.color=new Color(.48f,.82f,.60f);}

    private void BringIntoView(ScrollRect scroll,ActionBlock block)
    {
        var zone=scroll.viewport.GetComponent<ActionDropZone>();int index=zone.Items==null?-1:zone.Items.IndexOf(block);
        if(index<0||index>=zone.Tiles.Count)return;
        Canvas.ForceUpdateCanvases();var tile=zone.Tiles[index];float max=Mathf.Max(0,scroll.content.rect.width-scroll.viewport.rect.width);
        float x=Mathf.Clamp(tile.anchoredPosition.x-scroll.viewport.rect.width*.4f,0,max);scroll.content.anchoredPosition=new Vector2(-x,scroll.content.anchoredPosition.y);
    }

    public void BeginDrag(ActionDrag source,PointerEventData data)
    {
        if(source.Block==null&&source.Kind==ActionKind.Shoot&&!_manager.CanShootThisTurn)return;
        if(!CanEdit)return;CancelDrag();_dragBlock=source.Block;_dragSource=source.Source;_dragKind=source.Kind;
        _ghost=MazeUI.Box("DraggingAction",_canvas.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),MazeUI.Green);
        _ghost.sizeDelta=new Vector2(100,54);_ghost.gameObject.AddComponent<CanvasGroup>().blocksRaycasts=false;
        // Keep the drag receiver alive while opening/rebuilding a nested repeat editor.
        var receiver=_ghost.gameObject.AddComponent<ActionDrag>();receiver.Board=this;data.pointerDrag=_ghost.gameObject;
        MazeUI.Text("Name",_ghost,ActionPlan.Label(_dragBlock!=null?_dragBlock.Kind:_dragKind),16,TextAnchor.MiddleCenter);
        Drag(data);
    }

    public void Drag(PointerEventData data)
    {
        if(_ghost==null)return;_pointer=data.position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.transform as RectTransform,data.position,null,out var local);
        _ghost.anchoredPosition=local+new Vector2(12,28);DetectDrop(data.position);
    }

    private void DetectDrop(Vector2 screen)
    {
        _dropZone=null;
        for(int i=_zones.Count-1;i>=0;i--)
        {
            var zone=_zones[i];if(zone==null||!zone.isActiveAndEnabled||!RectTransformUtility.RectangleContainsScreenPoint(zone.transform as RectTransform,screen,null))continue;
            if(_dragBlock!=null&&zone.Owner!=null&&ActionPlan.Contains(_dragBlock,zone.Owner))continue;
            _dropZone=zone;_insertion=zone.Items.Count;
            for(int j=0;j<zone.Tiles.Count;j++)
            {
                var tile=zone.Tiles[j];var center=RectTransformUtility.WorldToScreenPoint(null,tile.TransformPoint(tile.rect.center));
                if(screen.x<center.x){_insertion=j;break;}
            }
            if(_marker==null)
            {
                _marker=MazeUI.Box("Insertion",_canvas.transform,Vector2.zero,Vector2.one,MazeUI.Green);
                _marker.GetComponent<Image>().raycastTarget=false;_marker.gameObject.AddComponent<LayoutElement>().preferredWidth=4;
            }
            _marker.SetParent(zone.Scroll.content,false);_marker.sizeDelta=new Vector2(4,0);_marker.SetSiblingIndex(_insertion);_marker.gameObject.SetActive(true);
            return;
        }
        if(_marker!=null)_marker.gameObject.SetActive(false);
    }

    public void EndDrag(PointerEventData data)
    {
        if(_ghost==null)return;DetectDrop(data.position);
        var target=_dropZone;var block=_dragBlock;var source=_dragSource;int index=_insertion;var kind=_dragKind;
        CancelDrag();
        if(!CanEdit||target==null)return;
        if(block==null&&StoredCount(_plan)>=ActionPlan.MaxBlocks)return;
        if(!MoveBlock(source,block,target.Items,target.Owner,index,kind))return;
        Changed();
    }

    public static bool MoveBlock(List<ActionBlock> source,ActionBlock block,List<ActionBlock> target,ActionBlock targetOwner,int insertion,ActionKind newKind=ActionKind.Up)
    {
        if(target==null||block!=null&&targetOwner!=null&&ActionPlan.Contains(block,targetOwner))return false;
        if(block!=null)
        {
            if(source==null)return false;int old=source.IndexOf(block);if(old<0)return false;
            if(source==target&&old<insertion)insertion--;source.RemoveAt(old);
        }
        else block=new ActionBlock(newKind);
        target.Insert(Mathf.Clamp(insertion,0,target.Count),block);return true;
    }

    private void CancelDrag()
    {
        if(_ghost!=null)Destroy(_ghost.gameObject);if(_marker!=null)Destroy(_marker.gameObject);
        _ghost=null;_marker=null;_dragBlock=null;_dragSource=null;_dropZone=null;_hoverRepeat=null;
    }

    private void ScrollToEnd(ScrollRect scroll){Canvas.ForceUpdateCanvases();scroll.horizontalNormalizedPosition=1;}
    private void ScrollBy(ScrollRect scroll,float delta)
    {
        float max=Mathf.Max(0,scroll.content.rect.width-scroll.viewport.rect.width);
        scroll.content.anchoredPosition=new Vector2(-Mathf.Clamp(-scroll.content.anchoredPosition.x+delta,0,max),scroll.content.anchoredPosition.y);
    }

    private Text LabelAt(Transform parent,string name,string text,Vector2 min,Vector2 max,int size,TextAnchor align=TextAnchor.MiddleLeft)
    {
        var holder=MazeUI.Rect(name,parent,min,max);return MazeUI.Text("Text",holder,text,size,align);
    }
    private void AddIcon(Transform parent,ActionKind kind,Vector2 min,Vector2 max,Color color)
    {
        var icon=MazeUI.Rect("Icon",parent,min,max).gameObject.AddComponent<ActionIcon>();icon.Kind=kind;icon.color=color;icon.raycastTarget=false;
    }
    public void Hide(){CancelDrag();if(_canvas!=null)_canvas.gameObject.SetActive(false);}
    private void OnDestroy(){if(_canvas!=null)Destroy(_canvas.gameObject);}
}

public class ActionDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public MovementBoard Board;
    public ActionBlock Block;
    public ActionKind Kind;
    public List<ActionBlock> Source;
    public void OnBeginDrag(PointerEventData data){Board.BeginDrag(this,data);}
    public void OnDrag(PointerEventData data){Board.Drag(data);}
    public void OnEndDrag(PointerEventData data){Board.EndDrag(data);}
}

public class ActionDropZone : MonoBehaviour
{
    public ScrollRect Scroll;
    public ActionBlock Owner;
    public List<ActionBlock> Items;
    public readonly List<RectTransform> Tiles = new List<RectTransform>();
}

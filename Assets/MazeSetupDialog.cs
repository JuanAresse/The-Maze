using System;
using UnityEngine;
using UnityEngine.UI;

public class MazeSetupDialog : MonoBehaviour
{
    private static MazeSetupDialog _open;
    private Canvas _canvas;
    private int _size;
    private Action _start;
    private Action _cancel;
    private readonly Button[] _choices = new Button[3];
    public static void Show(Action start, Action cancel)
    {
        if (_open != null) return;
        _open = new GameObject("MazeSizeSelection").AddComponent<MazeSetupDialog>();
        _open._start=start;_open._cancel=cancel;_open.Create();
    }
    private void Create()
    {
        _size=Array.IndexOf(GameOptions.Sizes,GameOptions.SavedSize)>=0?GameOptions.SavedSize:6;
        _canvas=MazeUI.Canvas("MazeSizeDialog",200);
        MazeUI.Box("Dim",_canvas.transform,Vector2.zero,Vector2.one,new Color(0,0,0,.75f));
        var panel=MazeUI.Box("Setup",_canvas.transform,new Vector2(.18f,.19f),new Vector2(.82f,.81f),MazeUI.Panel);
        Label(panel,"Title","Nueva partida",new Vector2(.05f,.79f),new Vector2(.95f,.96f),30);
        Label(panel,"Prompt","Elegí el tamaño del laberinto",new Vector2(.05f,.64f),new Vector2(.95f,.79f),22);
        for(int i=0;i<3;i++)
        {
            int index=i,size=GameOptions.Sizes[i];float x=.05f+i*.30f;
            _choices[i]=MazeUI.Button("Size_"+size,panel,$"{size} × {size}\n"+(i==0?"Clásico":i==1?"Recomendado":"Exploración"),new Vector2(x,.34f),new Vector2(x+.29f,.62f),()=>{_size=GameOptions.Sizes[index];Refresh();});
        }
        Label(panel,"Rules","8 acciones por turno · 1 disparo · 30 segundos para planificar",new Vector2(.05f,.21f),new Vector2(.95f,.33f),17);
        MazeUI.Button("CancelSetup",panel,"Volver",new Vector2(.05f,.04f),new Vector2(.33f,.20f),()=>Close(false));
        MazeUI.Button("StartMatch",panel,"Iniciar partida",new Vector2(.49f,.04f),new Vector2(.95f,.20f),()=>Close(true),MazeUI.Green);
        Refresh();
    }
    private void Refresh()
    {for(int i=0;i<3;i++)_choices[i].GetComponent<Image>().color=GameOptions.Sizes[i]==_size?new Color(.50f,.82f,.63f):MazeUI.Tile;}
    private void Close(bool start)
    {
        if(start)GameOptions.ChooseSize(_size);
        Action callback=start?_start:_cancel;
        Destroy(_canvas.gameObject);Destroy(gameObject);_open=null;callback?.Invoke();
    }
    private void Label(Transform parent,string name,string text,Vector2 min,Vector2 max,int size)
    {MazeUI.Text(name,MazeUI.Rect(name+"Area",parent,min,max),text,size,TextAnchor.MiddleCenter);}
}

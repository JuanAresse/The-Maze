using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public float barWorldWidth = .6f;
    private GameObject _canvas;
    private RectTransform _foreground;
    private Renderer[] _renderers;
    private void Awake()
    {
        currentHealth=maxHealth;
        _canvas=new GameObject("Health_"+name,typeof(RectTransform),typeof(Canvas));
        _canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
        var rect=_canvas.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(100,12);rect.localScale=Vector3.one*(barWorldWidth/100f);
        var background=MazeUI.Box("Background",rect,Vector2.zero,Vector2.one,new Color(.45f,.08f,.08f));
        _foreground=MazeUI.Box("Health",background,Vector2.zero,Vector2.one,new Color(.15f,.9f,.25f));
        _canvas.GetComponentsInChildren<Image>()[0].raycastTarget=false;_foreground.GetComponent<Image>().raycastTarget=false;
        CacheRenderers();
        UpdateBar();
    }
    public void TakeDamage(int amount)=>SetHealth(currentHealth-amount);
    private void SetHealth(int value)
    {
        currentHealth=Mathf.Clamp(value,0,maxHealth);UpdateBar();
        if(currentHealth<=0){var character=GetComponent<Character>();character?.manager?.PlayerLost(character);}
    }
    private void UpdateBar()
    {if(_foreground!=null){_foreground.anchorMax=new Vector2(maxHealth>0?(float)currentHealth/maxHealth:0,1);_foreground.offsetMin=Vector2.zero;_foreground.offsetMax=Vector2.zero;}}
    private void LateUpdate()
    {
        if(_canvas==null)return;
        Bounds bounds=new Bounds(transform.position,Vector3.zero);bool found=false;
        foreach(var renderer in _renderers)
            if(renderer.enabled){if(!found){bounds=renderer.bounds;found=true;}else bounds.Encapsulate(renderer.bounds);}
        _canvas.transform.position=new Vector3(bounds.center.x,bounds.max.y+.17f,bounds.center.z);
        foreach(var camera in Camera.allCameras)if(camera.enabled){_canvas.transform.rotation=camera.transform.rotation;break;}
    }
    public void CacheRenderers() => _renderers=GetComponentsInChildren<Renderer>();
    private void OnDestroy(){if(_canvas!=null)Destroy(_canvas);}
}

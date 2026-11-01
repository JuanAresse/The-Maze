using UnityEngine;

public class WorldLabel : MonoBehaviour
{
    private Transform _target;
    private Vector3 _offset;
    public static void Create(Transform target,string text,Vector3 offset,Color color)
    {
        var canvas=new GameObject("Label_"+text,typeof(RectTransform),typeof(Canvas));
        canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
        var rect=canvas.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(170,36);rect.localScale=Vector3.one*.0035f;
        var label=MazeUI.Text("Text",rect,text,24,TextAnchor.MiddleCenter);label.color=color;
        var shadow=label.gameObject.AddComponent<UnityEngine.UI.Shadow>();shadow.effectColor=new Color(0,0,0,.85f);shadow.effectDistance=new Vector2(1,-1);
        var follow=canvas.AddComponent<WorldLabel>();follow._target=target;follow._offset=offset;
    }
    private void LateUpdate()
    {
        if(_target==null){Destroy(gameObject);return;}
        transform.position=_target.position+_offset;
        foreach(var camera in Camera.allCameras)if(camera.enabled)
        {
            transform.rotation=camera.transform.rotation;
            float distance=Vector3.Distance(transform.position,camera.transform.position);
            float scale=2*distance*Mathf.Tan(camera.fieldOfView*.5f*Mathf.Deg2Rad)*12/Mathf.Max(1,camera.pixelHeight)/24;
            transform.localScale=Vector3.one*Mathf.Clamp(scale,.002f,.025f);
            break;
        }
    }
}

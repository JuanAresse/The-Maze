using UnityEngine;
using UnityEngine.UI;

// Small vector pictograms remain sharp at any CanvasScaler resolution.
[RequireComponent(typeof(CanvasRenderer))]
public class ActionIcon : MaskableGraphic
{
    public ActionKind Kind;
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (Kind <= ActionKind.Right)
        {
            float angle = Kind == ActionKind.Up ? 0 : Kind == ActionKind.Down ? 180 : Kind == ActionKind.Left ? 90 : -90;
            Vector2[] arrow = { new Vector2(.39f,.12f), new Vector2(.61f,.12f), new Vector2(.61f,.55f), new Vector2(.88f,.55f), new Vector2(.50f,.92f), new Vector2(.12f,.55f), new Vector2(.39f,.55f) };
            for (int i = 0; i < arrow.Length; i++) arrow[i] = (Vector2)(Quaternion.Euler(0,0,angle) * (arrow[i] - Vector2.one * .5f)) + Vector2.one * .5f;
            for (int i = 0; i < arrow.Length; i++) Triangle(mesh, new Vector2(.5f,.5f), arrow[i], arrow[(i+1)%arrow.Length]);
        }
        else if (Kind == ActionKind.Shoot)
        {
            for (int i=0;i<32;i++) { float a=i*Mathf.PI/16, b=(i+1)*Mathf.PI/16; Line(mesh,new Vector2(.5f+Mathf.Cos(a)*.29f,.5f+Mathf.Sin(a)*.29f),new Vector2(.5f+Mathf.Cos(b)*.29f,.5f+Mathf.Sin(b)*.29f),.06f); }
            Line(mesh,new Vector2(.5f,.03f),new Vector2(.5f,.34f),.07f); Line(mesh,new Vector2(.5f,.66f),new Vector2(.5f,.97f),.07f);
            Line(mesh,new Vector2(.03f,.5f),new Vector2(.34f,.5f),.07f); Line(mesh,new Vector2(.66f,.5f),new Vector2(.97f,.5f),.07f);
        }
        else if (Kind == ActionKind.Wait)
        {
            Line(mesh,new Vector2(.2f,.9f),new Vector2(.8f,.9f),.07f); Line(mesh,new Vector2(.2f,.1f),new Vector2(.8f,.1f),.07f);
            Line(mesh,new Vector2(.25f,.85f),new Vector2(.75f,.15f),.055f); Line(mesh,new Vector2(.75f,.85f),new Vector2(.25f,.15f),.055f);
            Triangle(mesh,new Vector2(.32f,.2f),new Vector2(.68f,.2f),new Vector2(.5f,.43f));
        }
        else
        {
            for(int i=0;i<22;i++){float a=(i*13+35)*Mathf.Deg2Rad,b=(i*13+48)*Mathf.Deg2Rad;Line(mesh,new Vector2(.5f+Mathf.Cos(a)*.30f,.5f+Mathf.Sin(a)*.30f),new Vector2(.5f+Mathf.Cos(b)*.30f,.5f+Mathf.Sin(b)*.30f),.085f);}
            Triangle(mesh,new Vector2(.88f,.78f),new Vector2(.59f,.78f),new Vector2(.88f,.49f));
        }
    }

    private Vector2 Point(Vector2 p)
    {
        var rect=rectTransform.rect;
        return rect.center+(p-Vector2.one*.5f)*Mathf.Min(rect.width,rect.height);
    }
    private void Triangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c)
    {
        int i=mesh.currentVertCount; mesh.AddVert(Point(a),color,Vector2.zero);mesh.AddVert(Point(b),color,Vector2.zero);mesh.AddVert(Point(c),color,Vector2.zero);mesh.AddTriangle(i,i+1,i+2);
    }
    private void Line(VertexHelper mesh,Vector2 a,Vector2 b,float width)
    {
        Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;
        Triangle(mesh,a+n,a-n,b+n);Triangle(mesh,b+n,a-n,b-n);
    }
}

#if UNITY_EDITOR

using UnityEngine;

// source: http://martinecker.com/martincodes/unity-editor-window-zooming/

//missing features from what i did : endGroup, beginGroup

public static class RectExtensions
{
    public static Vector2 TopLeft(this Rect rect)
    {
        return new Vector2(rect.xMin, rect.yMin);
    }

    public static Rect ScaleSizeBy(this Rect rect, float scale)
    {
        return rect.ScaleSizeBy(scale, rect.center);
    }
    public static Rect ScaleSizeBy(this Rect rect, float scale, Vector2 pivotPoint)
    {
        Rect result = rect;
        result.x -= pivotPoint.x;
        result.y -= pivotPoint.y;

        result.xMin *= scale;
        result.xMax *= scale;
        result.yMin *= scale;
        result.yMax *= scale;

        result.x += pivotPoint.x;
        result.y += pivotPoint.y;


        return result;
    }



    public static Rect ScaleNodeWindow(this Rect rect, float scale)
    {
        return rect.ScaleNodeWindow(scale, rect.center);
    }
    public static Rect ScaleNodeWindow(this Rect rect, float scale, Vector2 pivotPoint)
    {
        Rect result = rect;
        result.x -= pivotPoint.x;
        result.y -= pivotPoint.y;

        result.xMin *= scale;
        result.xMax *= scale;
        result.yMin *= scale;
        result.yMax *= scale;

        result.x += pivotPoint.x;
        result.y += pivotPoint.y;

        result.x *= scale;
        result.y *= scale;

        return result;
    }

    /// <summary>
    /// give this 2 rect that have the same origin and the wame width, and this return the Rect of coord: 
    /// Pos: x, y + other.size.y
    /// Size: me.size.y - other.size.y
    /// </summary>
    /// <returns></returns>
    public static Rect Minus(this Rect me, Rect other)
    {
        Rect outPut = me;
        outPut.position += other.position - me.position + new Vector2(0, other.size.y);
        outPut.size -= new Vector2(0, other.size.y);

        return outPut;
    }
}

public class EditorZoomArea
{
    private const float kEditorWindowTabHeight = 21.0f;
    private static Matrix4x4 _prevGuiMatrix;

    public static Rect Begin(float zoomScale, Rect screenCoordsArea)
    {
        GUI.EndGroup();        // End the group Unity begins automatically for an EditorWindow to clip out the window tab. This allows us to draw outside of the size of the EditorWindow.

        Rect clippedArea = screenCoordsArea.ScaleSizeBy(1.0f / zoomScale, screenCoordsArea.TopLeft());
        clippedArea.y += kEditorWindowTabHeight;
        GUI.BeginGroup(clippedArea);

        _prevGuiMatrix = GUI.matrix;
        Matrix4x4 translation = Matrix4x4.TRS(clippedArea.TopLeft(), Quaternion.identity, Vector3.one);
        Matrix4x4 scale = Matrix4x4.Scale(new Vector3(zoomScale, zoomScale, 1.0f));
        GUI.matrix = translation * scale * translation.inverse * GUI.matrix;

        return clippedArea;
    }

    public static void End()
    {
        GUI.matrix = _prevGuiMatrix;
        GUI.EndGroup();
        GUI.BeginGroup(new Rect(0.0f, kEditorWindowTabHeight, Screen.width, Screen.height));
    }
}
#endif

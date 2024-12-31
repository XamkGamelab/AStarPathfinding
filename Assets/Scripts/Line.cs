using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Line
{
    const float verticalLineGradient = 1e5f;
    float gradient;
    float y_intercept;
    Vector2 linePoint_1, linePoint_2;
    float gradientPerpendicular;

    bool approachSide;
    public Line(Vector2 linePoint, Vector2 linePointPerpendicular)
    {
        float dx = linePoint.x - linePointPerpendicular.x;
        float dy = linePoint.y - linePointPerpendicular.y;
        if (dx == 0) gradientPerpendicular = verticalLineGradient;
        else gradientPerpendicular = dy / dx;
        if(gradientPerpendicular==0) gradient = verticalLineGradient;
        else gradient = -1 / gradientPerpendicular;

        y_intercept = linePoint.y - gradient * linePoint.x;
        linePoint_1=linePoint;
        linePoint_2=linePoint+new Vector2(1,gradient);
        approachSide = true;
        approachSide = GetSide(linePointPerpendicular);
    }
    bool GetSide(Vector2 p)
    {
        return (p.x-linePoint_1.x)*(linePoint_2.y-linePoint_1.y)>(p.y-linePoint_1.y)*(linePoint_2.x-linePoint_1.x);
    }
    public bool HasCrossedLine(Vector2 p)
    {
        return GetSide(p) != approachSide;
    }
    public float DistanceFromPoint(Vector2 p)
    {
        float yInterceptPerpendicular = p.y - gradientPerpendicular * p.x;
        float intersectX = (yInterceptPerpendicular - y_intercept) / (gradient - gradientPerpendicular);
        float intersectY = gradient * intersectX + y_intercept;
        return Vector2.Distance(p,new Vector2(intersectX,intersectY));
    }
    public void DrawWithGizmos(float length)
    {
        Vector3 lineDir = new Vector3(1,0,gradient).normalized;
        Vector3 lineCenter=new Vector3(linePoint_1.x,0,linePoint_1.y)+Vector3.up;
        Gizmos.DrawLine(lineCenter-lineDir*length/2f, lineCenter+lineDir*length/2f);
    }
}

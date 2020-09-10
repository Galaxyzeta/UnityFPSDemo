using UnityEngine;

public class CrossHairData : MonoBehaviour {
    
    public enum CrossHairType {
        CROSSED, CIRCLE, DOT
    }

    public float crossHairLength = 2.5f;
    public float minScreenRadius = 1.5f;
    public float maxScreenRadius = 8f;
    public float lerpAcceleration = 2f;
    public CrossHairType type = CrossHairType.CROSSED;
    public Material material;
    public Color starColor;
    private bool isVisible = true;

    public float targetRadius {get; private set;}
    public float currentRadius {get; private set;}      // Current radius is a speed-lerp of target radius. We use a fake lerp value to smooth the tween transition !
 
    public void SetRadius(float radius) {
        this.targetRadius = radius;
    }

    public void SetProgressRadius(float progress) {
        this.targetRadius = Mathf.Lerp(minScreenRadius, maxScreenRadius, progress);
    }

    public void SetVisible(bool isVisible) {
        this.isVisible = isVisible;
    }

    public void DrawLine(Vector2 center) {
        if(!isVisible) {
            return;
        }
        GL.PushMatrix();
        material.SetPass(0);
        GL.LoadOrtho();
        switch(type) {
            case CrossHairType.CROSSED: {
                DrawCrossed(center);
                break;
            }
            case CrossHairType.CIRCLE: {
                DrawCircle(center);
                break;
            }
        }
        GL.PopMatrix();
    }

    // Line helper
    private void DrawLine(Vector2 start,Vector2 end) {
        GL.Vertex3(start.x / Screen.width, start.y / Screen.height, 0);
        GL.Vertex3(end.x / Screen.width, end.y / Screen.height, 0);
    }

    // Draw a crossed mark
    private void DrawCrossed(Vector2 center) {
        float offset = crossHairLength + currentRadius;
        GL.Begin(GL.LINES);
        DrawLine(new Vector2(center.x, center.y + currentRadius), new Vector2(center.x, center.y + offset));
        DrawLine(new Vector2(center.x, center.y - currentRadius), new Vector2(center.x, center.y - offset));
        DrawLine(new Vector2(center.x + currentRadius, center.y), new Vector2(center.x + offset, center.y));
        DrawLine(new Vector2(center.x - currentRadius, center.y), new Vector2(center.x - offset, center.y));
        GL.End();
    }

    // Draw a circle mark
    private void DrawCircle(Vector2 center) {
        GL.Begin(GL.LINES);
        GL.Color(starColor);
        const float poly = 10;
        float angle = (360 / poly) * Mathf.PI / 180;
        float minRad = 5f;
        float calcRadius = minRad + currentRadius;
        for(int i=0; i<poly; i++) {
            Vector2 point1 = new Vector2(center.x + calcRadius * Mathf.Cos(angle*i), center.y + calcRadius * Mathf.Sin(angle*i));
            Vector2 point2 = new Vector2(center.x + calcRadius * Mathf.Cos(angle*i+angle), center.y + calcRadius * Mathf.Sin(angle*i+angle));
            DrawLine(point1, point2);
        }
        GL.End();
    }

    // === LIFE SPAN ===
    private void Start() {
        targetRadius = minScreenRadius;
        material.color = this.starColor;
    }

    private void Update() {
        currentRadius = TweenLerpUtil.SpeedLerp(currentRadius, targetRadius, lerpAcceleration);
    }

}
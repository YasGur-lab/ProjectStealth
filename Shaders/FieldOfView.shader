Shader "FieldOfView" {
	Properties{  
		// Properties of the material
		_MainTex("Base (RGB)", 2D) = "white" {}
		_FOVColor("Field Of View Color", Color) = (1, 1, 1)
		_MainColor("MainColor", Color) = (1, 1, 1)
		_Position1("Position1",  Vector) = (0,0,0)
		_Position2("Position2",  Vector) = (0,0,0)
		_Position3("Position3",  Vector) = (0,0,0)
		_Direction1("Direction1",  Vector) = (0,0,0)
		_Direction2("Direction2",  Vector) = (0,0,0)
		_Direction3("Direction3",  Vector) = (0,0,0)
	}
		SubShader{
		Tags{ "RenderType" = "Diffuse" }
		// https://docs.unity3d.com/Manual/SL-SurfaceShaders.html
		CGPROGRAM
#pragma surface surf Lambert

	sampler2D _MainTex;
	//https://docs.unity3d.com/Manual/SL-DataTypesAndPrecision.html
	fixed3 _FOVColor; //Precision
	fixed3 _MainColor;
	float3 _Position1;
	float3 _Position2;
	float3 _Position3;
	float3 _Direction1;
	float3 _Direction2;
	float3 _Direction3;

	// Values that interpolated from vertex data.
	struct Input {
		float2 uv_MainTex;
		float3 worldPos;
	};

	// Barycentric coordinates
	// http://mathworld.wolfram.com/BarycentricCoordinates.html
	bool isPointInTriangle(float2 p1, float2 p2, float2 p3, float2 pointInQuestion)
	{
		float denominator = ((p2.y - p3.y) * (p1.x - p3.x) + (p3.x - p2.x) * (p1.y - p3.y));
		float a = ((p2.y -p3.y) * (pointInQuestion.x - p3.x) + (p3.x - p2.x) * (pointInQuestion.y - p3.y)) / denominator;
		float b  = ((p3.y - p1.y) * (pointInQuestion.x - p3.x) + (p1.x - p3.x) * (pointInQuestion.y - p3.y)) / denominator;
		float c  = 1 - a - b;

		return 0 <= a && a <= 1 && 0 <= b && b <= 1 && 0 <= c && c <= 1;
	}

	bool isPointInTheCircle(float2 circleCenterPoint, float2 thisPoint, float radius)
	{
		return distance(circleCenterPoint, thisPoint) <= radius;
	}
	
	void surf(Input IN, inout SurfaceOutput o) {
		half4 c = tex2D(_MainTex, IN.uv_MainTex);
		
		float3 basePoint1 = _Position1.xyz;
		basePoint1.y = 0;

		float3 basePoint2 = _Position2.xyz;
		basePoint2.y = 0;

		float3 basePoint3 = _Position3.xyz;
		basePoint3.y = 0;

		float offsetAngle = 45.0;
		float offsetAngleInRadians = offsetAngle * (3.14 / 180);
		float distance = 10.0;

		//cos(adj / hypo) to find hypo /// hypo = adj / cos()

		float adjustedDistance = distance / cos(offsetAngleInRadians);

		float3 aiDir1 = _Direction1.xyz;
		float3 aiDir2 = _Direction2.xyz;
		float3 aiDir3 = _Direction3.xyz;

		float3 camDir = -1 * UNITY_MATRIX_IT_MV[2].xyz;
		float viewAngle1 = atan2(aiDir1.z, aiDir1.x);
		float viewAngle2 = atan2(aiDir2.z, aiDir2.x);
		float viewAngle3 = atan2(aiDir3.z, aiDir3.x);

		float3 centerPoint1 = (float3(cos(viewAngle1), 0, sin(viewAngle1)) * distance) + basePoint1;
		float3 rightPoint1 = (float3(cos(viewAngle1 + offsetAngleInRadians), 0, sin(viewAngle1 + offsetAngleInRadians)) * adjustedDistance) + basePoint1;
		float3 leftPoint1 = (float3(cos(viewAngle1 + -offsetAngleInRadians), 0, sin(viewAngle1 + -offsetAngleInRadians)) * adjustedDistance) + basePoint1;

		float3 centerPoint2 = (float3(cos(viewAngle2), 0, sin(viewAngle2)) * distance) + basePoint2;
		float3 rightPoint2 = (float3(cos(viewAngle2 + offsetAngleInRadians), 0, sin(viewAngle2 + offsetAngleInRadians)) * adjustedDistance) + basePoint2;
		float3 leftPoint2 = (float3(cos(viewAngle2 + -offsetAngleInRadians), 0, sin(viewAngle2 + -offsetAngleInRadians)) * adjustedDistance) + basePoint2;

		float3 centerPoint3 = (float3(cos(viewAngle3), 0, sin(viewAngle3)) * distance) + basePoint3;
		float3 rightPoint3 = (float3(cos(viewAngle3 + offsetAngleInRadians), 0, sin(viewAngle3 + offsetAngleInRadians)) * adjustedDistance) + basePoint3;
		float3 leftPoint3 = (float3(cos(viewAngle3 + -offsetAngleInRadians), 0, sin(viewAngle3 + -offsetAngleInRadians)) * adjustedDistance) + basePoint3;


		float3 pointInQuestion = IN.worldPos;

		c.rgb *= _MainColor;

		//if (isPointInTheCircle(basePoint.xz, pointInQuestion.xz, 0.3) || 
		//	isPointInTheCircle(centerPoint.xz, pointInQuestion.xz, 0.3) || 
		//	isPointInTheCircle(rightPoint.xz, pointInQuestion.xz, 0.3) || 
		//	isPointInTheCircle(leftPoint.xz, pointInQuestion.xz, 0.3))
		if(isPointInTriangle(rightPoint1.xz, leftPoint1.xz, basePoint1.xz, pointInQuestion.xz) && // if less than X degrees, pls tint
			isPointInTheCircle(basePoint1.xz, pointInQuestion.xz, distance) ||
			isPointInTriangle(rightPoint2.xz, leftPoint2.xz, basePoint2.xz, pointInQuestion.xz) && // if less than X degrees, pls tint
			isPointInTheCircle(basePoint2.xz, pointInQuestion.xz, distance) ||
			isPointInTriangle(rightPoint3.xz, leftPoint3.xz, basePoint3.xz, pointInQuestion.xz) && // if less than X degrees, pls tint
			isPointInTheCircle(basePoint3.xz, pointInQuestion.xz, distance))
		{
			o.Albedo = c.rgb* _FOVColor;
		}
		else
		{
			o.Albedo = c.rgb;
		}

		o.Alpha = c.a;
	}
	ENDCG
	}
		FallBack "Diffuse" //If we cannot use the subshader on specific hardware we will fallback to Diffuse shader
}

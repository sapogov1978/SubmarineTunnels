Shader "Custom/TunnelMask"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MaskColor ("Mask Color", Color) = (0,0,0,1)
        _FeatherDistance ("Feather Distance", Float) = 0.2
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MaskColor;
            float _FeatherDistance;

            // Эти параметры будут передаваться из TunnelMaskController
            float _LeftWallX;
            float _RightWallX;
            float _TunnelCenterY;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Получаем мировую позицию пикселя
                float worldX = i.worldPos.x;
                
                // Вычисляем расстояние до стен туннеля
                float distToLeftWall = worldX - _LeftWallX;
                float distToRightWall = _RightWallX - worldX;
                
                // Минимальное расстояние до ближайшей стены
                float distToTunnel = min(distToLeftWall, distToRightWall);
                
                // Если пиксель вне туннеля - затемняем
                float alpha = 0.0;
                if (distToTunnel < 0)
                {
                    // Вне туннеля - полная маска
                    alpha = 1.0;
                }
                else if (distToTunnel < _FeatherDistance)
                {
                    // Мягкий переход (feathering)
                    alpha = 1.0 - (distToTunnel / _FeatherDistance);
                }
                
                return float4(_MaskColor.rgb, alpha * _MaskColor.a);
            }
            ENDPROGRAM
        }
    }
}
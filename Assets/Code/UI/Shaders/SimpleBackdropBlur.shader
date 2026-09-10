// Shader de UI para un panel de "cristal esmerilado": difumina la textura que se le asigne
// (una RenderTexture rellenada por una camara aparte). Mismo esqueleto que el UI/Default de
// Unity (UnityCG.cginc/UnityUI.cginc, funciona igual bajo cualquier Render Pipeline, es lo
// mismo que ya usa toda la UI del proyecto). No depende de Renderer Features ni de texturas
// globales -- _MainTex es sencillamente la textura que le pongas al RawImage.
//
// Cuatro variantes de blur, elegidas por _BlurMode:
//   Uniform     - el clasico, difumina por igual en todas direcciones.
//   Directional - solo en un eje/angulo (tipo motion blur).
//   Radial      - crece con la distancia a un punto central (tipo zoom/velocidad).
//   TiltShift   - una franja se queda nitida, el resto se difumina segun se aleja de ella.
Shader "UI/SimpleBackdropBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture (RenderTexture)", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Space(10)]
        [KeywordEnum(Uniform, Directional, Radial, TiltShift)] _BlurMode ("Modo de blur", Float) = 0

        [Space(10)]
        _BlurRadius ("Radio", Range(0, 10)) = 2
        _BlurIterations ("Iteraciones / muestras", Range(1, 8)) = 1

        [Space(10)]
        _BlurAngle ("Direccional: angulo (grados)", Range(0, 360)) = 0

        [Space(10)]
        _BlurCenter ("Radial: centro (UV)", Vector) = (0.5, 0.5, 0, 0)

        [Space(10)]
        _TiltShiftCenter ("Tilt-shift: centro Y (UV)", Range(0, 1)) = 0.5
        _TiltShiftWidth ("Tilt-shift: ancho nitido", Range(0, 1)) = 0.2
        _TiltShiftSoftness ("Tilt-shift: suavizado del borde", Range(0.01, 1)) = 0.2

        [Space(10)]
        _TintColor ("Velo de color", Color) = (0, 0, 0, 0)
        _Brightness ("Brillo extra (aclarar, no solo oscurecer)", Range(-1, 1)) = 0
        _Saturation ("Saturacion (0 = blanco y negro)", Range(0, 1)) = 1

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _BLURMODE_UNIFORM _BLURMODE_DIRECTIONAL _BLURMODE_RADIAL _BLURMODE_TILTSHIFT
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float4    _MainTex_TexelSize;
            fixed4    _Color;
            float     _BlurRadius;
            float     _BlurIterations;
            float     _BlurAngle;
            float4    _BlurCenter;
            float     _TiltShiftCenter;
            float     _TiltShiftWidth;
            float     _TiltShiftSoftness;
            fixed4    _TintColor;
            float     _Brightness;
            float     _Saturation;
            float4    _ClipRect;

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.worldPosition = IN.vertex;
                OUT.vertex        = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord      = TRANSFORM_TEX(IN.texcoord, _MainTex);
                OUT.color         = IN.color * _Color;
                return OUT;
            }

            // --- modos de blur ---

            // Multi-tap 3x3, pesos tipo gaussiano, a un radio de texel concreto.
            fixed4 Blur3x3(float2 uv, float2 texel)
            {
                fixed4 sum = 0;
                sum += tex2D(_MainTex, uv + texel * float2(-1, -1)) * 0.0625;
                sum += tex2D(_MainTex, uv + texel * float2( 0, -1)) * 0.125;
                sum += tex2D(_MainTex, uv + texel * float2( 1, -1)) * 0.0625;
                sum += tex2D(_MainTex, uv + texel * float2(-1,  0)) * 0.125;
                sum += tex2D(_MainTex, uv + texel * float2( 0,  0)) * 0.25;
                sum += tex2D(_MainTex, uv + texel * float2( 1,  0)) * 0.125;
                sum += tex2D(_MainTex, uv + texel * float2(-1,  1)) * 0.0625;
                sum += tex2D(_MainTex, uv + texel * float2( 0,  1)) * 0.125;
                sum += tex2D(_MainTex, uv + texel * float2( 1,  1)) * 0.0625;
                return sum;
            }

            // Varias "coronas" de blur a radios crecientes, promediadas: mas suave y mas
            // fuerte que subir solo el radio (menos bandas visibles).
            fixed4 BlurUniform(float2 uv)
            {
                float2 baseTexel = _MainTex_TexelSize.xy * max(_BlurRadius, 0.0001);
                int iterations = (int) max(_BlurIterations, 1);

                fixed4 sum = 0;
                for (int i = 1; i <= iterations; i++)
                    sum += Blur3x3(uv, baseTexel * i);

                return sum / iterations;
            }

            // Muestras en linea recta segun _BlurAngle, con peso decreciente segun distancia.
            fixed4 BlurDirectional(float2 uv)
            {
                float  rad  = radians(_BlurAngle);
                float2 dir  = float2(cos(rad), sin(rad));
                float2 step = dir * _MainTex_TexelSize.xy * max(_BlurRadius, 0.0001);
                int    taps = (int) max(_BlurIterations, 1);

                fixed4 sum    = tex2D(_MainTex, uv);
                float  weight = 1;

                for (int i = 1; i <= taps; i++)
                {
                    float w = 1.0 - (float) i / (taps + 1);
                    sum    += tex2D(_MainTex, uv + step * i) * w;
                    sum    += tex2D(_MainTex, uv - step * i) * w;
                    weight += w * 2;
                }

                return sum / weight;
            }

            // Muestras hacia _BlurCenter, con peso decreciente: sensacion de velocidad/zoom.
            fixed4 BlurRadial(float2 uv)
            {
                float2 dir  = (uv - _BlurCenter.xy) * (_BlurRadius * 0.005);
                int    taps = (int) max(_BlurIterations, 1);

                fixed4 sum    = tex2D(_MainTex, uv);
                float  weight = 1;

                for (int i = 1; i <= taps; i++)
                {
                    float t = (float) i / taps;
                    float w = 1.0 - t;
                    sum    += tex2D(_MainTex, uv - dir * t) * w;
                    weight += w;
                }

                return sum / weight;
            }

            // Franja nitida centrada en _TiltShiftCenter (eje Y); fuera de ese ancho se mezcla
            // hacia el blur uniforme segun la distancia a la franja.
            fixed4 BlurTiltShift(float2 uv)
            {
                float dist       = abs(uv.y - _TiltShiftCenter);
                float blurAmount = smoothstep(_TiltShiftWidth, _TiltShiftWidth + max(_TiltShiftSoftness, 0.001), dist);

                fixed4 sharp = tex2D(_MainTex, uv);
                if (blurAmount <= 0.0001) return sharp;

                fixed4 blurred = BlurUniform(uv);
                return lerp(sharp, blurred, blurAmount);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                #if defined(_BLURMODE_DIRECTIONAL)
                    fixed4 result = BlurDirectional(IN.texcoord);
                #elif defined(_BLURMODE_RADIAL)
                    fixed4 result = BlurRadial(IN.texcoord);
                #elif defined(_BLURMODE_TILTSHIFT)
                    fixed4 result = BlurTiltShift(IN.texcoord);
                #else
                    fixed4 result = BlurUniform(IN.texcoord);
                #endif

                // Velo de color, mezclado por su propio alpha.
                result.rgb = lerp(result.rgb, _TintColor.rgb, _TintColor.a);

                // Saturacion: 0 = blanco y negro (luminancia), 1 = color original.
                fixed luma = dot(result.rgb, fixed3(0.299, 0.587, 0.114));
                result.rgb = lerp(fixed3(luma, luma, luma), result.rgb, _Saturation);

                // Brillo aditivo: el Color/Tint normal solo puede oscurecer (multiplica), esto
                // permite tambien aclarar el resultado (util para el look "cristal").
                result.rgb = saturate(result.rgb + _Brightness);

                fixed4 color = result * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                    color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}

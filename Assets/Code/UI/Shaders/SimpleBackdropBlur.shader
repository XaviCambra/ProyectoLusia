// Shader de UI para un panel de "cristal esmerilado": difumina la textura que se le asigne
// (una RenderTexture rellenada por una camara aparte). Mismo esqueleto que el UI/Default de
// Unity (UnityCG.cginc/UnityUI.cginc, funciona igual bajo cualquier Render Pipeline, es lo
// mismo que ya usa toda la UI del proyecto). No depende de Renderer Features ni de texturas
// globales -- _MainTex es sencillamente la textura que le pongas al RawImage.
//
// Tres variantes de blur, elegidas por _BlurMode:
//   Uniform     - el clasico, difumina por igual en todas direcciones.
//   Directional - solo en un eje/angulo (tipo motion blur).
//   Radial      - crece con la distancia a un punto central (tipo zoom/velocidad).
// _BlurHighQuality (modo Uniform): 9 muestras (mas suave) o 5 (mas barato).
Shader "UI/SimpleBackdropBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture (RenderTexture)", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Space(10)]
        [KeywordEnum(Uniform, Directional, Radial)] _BlurMode ("Modo de blur", Float) = 0

        [Space(10)]
        _BlurRadius ("Radio", Range(0, 8)) = 2
        _BlurIterations ("Iteraciones / muestras", Range(1, 32)) = 1
        [Toggle(_BLURQUALITY_HIGH)] _BlurHighQuality ("Calidad alta (9 muestras en vez de 5, modo Uniform)", Float) = 1

        [Space(10)]
        _BlurAngle ("Direccional: angulo (grados)", Range(0, 360)) = 0
        _DirectionalFalloff ("Direccional: caida del peso", Range(0.1, 4)) = 1

        [Space(10)]
        _BlurCenter ("Radial: centro (UV)", Vector) = (0.5, 0.5, 0, 0)
        _RadialFalloff ("Radial: caida del peso", Range(0.1, 4)) = 1
        _RadialInnerRadius ("Radial: radio interior nitido", Range(0, 1)) = 0

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
            #pragma shader_feature_local _BLURMODE_UNIFORM _BLURMODE_DIRECTIONAL _BLURMODE_RADIAL
            #pragma shader_feature_local _ _BLURQUALITY_HIGH
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
            float     _DirectionalFalloff;
            float4    _BlurCenter;
            float     _RadialFalloff;
            float     _RadialInnerRadius;
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

            // Multi-tap a un radio de texel concreto, pesos tipo gaussiano. Calidad alta = 9
            // muestras (incluye esquinas); calidad baja = 5 muestras (solo centro + cruz),
            // mas barato, util cuando el rendimiento importa mas que la suavidad.
            fixed4 Blur3x3(float2 uv, float2 texel)
            {
                fixed4 sum = 0;
                #if defined(_BLURQUALITY_HIGH)
                    sum += tex2D(_MainTex, uv + texel * float2(-1, -1)) * 0.0625;
                    sum += tex2D(_MainTex, uv + texel * float2( 0, -1)) * 0.125;
                    sum += tex2D(_MainTex, uv + texel * float2( 1, -1)) * 0.0625;
                    sum += tex2D(_MainTex, uv + texel * float2(-1,  0)) * 0.125;
                    sum += tex2D(_MainTex, uv + texel * float2( 0,  0)) * 0.25;
                    sum += tex2D(_MainTex, uv + texel * float2( 1,  0)) * 0.125;
                    sum += tex2D(_MainTex, uv + texel * float2(-1,  1)) * 0.0625;
                    sum += tex2D(_MainTex, uv + texel * float2( 0,  1)) * 0.125;
                    sum += tex2D(_MainTex, uv + texel * float2( 1,  1)) * 0.0625;
                #else
                    sum += tex2D(_MainTex, uv + texel * float2( 0, -1)) * 0.16667;
                    sum += tex2D(_MainTex, uv + texel * float2(-1,  0)) * 0.16667;
                    sum += tex2D(_MainTex, uv + texel * float2( 0,  0)) * 0.33333;
                    sum += tex2D(_MainTex, uv + texel * float2( 1,  0)) * 0.16667;
                    sum += tex2D(_MainTex, uv + texel * float2( 0,  1)) * 0.16667;
                #endif
                return sum;
            }

            // Varias "coronas" de blur a radios crecientes, promediadas: mas suave y mas
            // fuerte que subir solo el radio (menos bandas visibles).
            fixed4 BlurUniform(float2 uv)
            {
                float2 baseTexel = _MainTex_TexelSize.xy * max(_BlurRadius, 0.0001);
                int iterations = (int) max(_BlurIterations, 1);

                fixed4 sum = 0;
                [loop]
                for (int i = 1; i <= iterations; i++)
                    sum += Blur3x3(uv, baseTexel * i);

                return sum / iterations;
            }

            // Muestras en linea recta segun _BlurAngle, con peso decreciente segun distancia.
            // _MainTex_TexelSize.xy ya corrige el aspecto de la textura (no cuadrada): un
            // angulo "recto" en pantalla se queda recto, no se tuerce hacia el lado mas largo.
            fixed4 BlurDirectional(float2 uv)
            {
                float  rad  = radians(_BlurAngle);
                float2 dir  = float2(cos(rad), sin(rad));
                float2 step = dir * _MainTex_TexelSize.xy * max(_BlurRadius, 0.0001);
                int    taps = (int) max(_BlurIterations, 1);

                fixed4 sum    = tex2D(_MainTex, uv);
                float  weight = 1;

                [loop]
                for (int i = 1; i <= taps; i++)
                {
                    float t = (float) i / (taps + 1);
                    float w = pow(1.0 - t, max(_DirectionalFalloff, 0.0001));
                    sum    += tex2D(_MainTex, uv + step * i) * w;
                    sum    += tex2D(_MainTex, uv - step * i) * w;
                    weight += w * 2;
                }

                return sum / weight;
            }

            // Muestras hacia _BlurCenter, con peso decreciente: sensacion de velocidad/zoom.
            // _RadialInnerRadius deja un circulo nitido sin blur antes de que empiece a crecer
            // (tipo "vision de tunel"), _RadialFalloff controla como de rapido cae el peso.
            fixed4 BlurRadial(float2 uv)
            {
                float2 toCenter = uv - _BlurCenter.xy;
                float  dist     = length(toCenter);

                if (dist <= _RadialInnerRadius) return tex2D(_MainTex, uv);

                float2 dir  = toCenter * (_BlurRadius * 0.005);
                int    taps = (int) max(_BlurIterations, 1);

                fixed4 sum    = tex2D(_MainTex, uv);
                float  weight = 1;

                [loop]
                for (int i = 1; i <= taps; i++)
                {
                    float t = (float) i / taps;
                    float w = pow(1.0 - t, max(_RadialFalloff, 0.0001));
                    sum    += tex2D(_MainTex, uv - dir * t) * w;
                    weight += w;
                }

                return sum / weight;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                #if defined(_BLURMODE_DIRECTIONAL)
                    fixed4 result = BlurDirectional(IN.texcoord);
                #elif defined(_BLURMODE_RADIAL)
                    fixed4 result = BlurRadial(IN.texcoord);
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

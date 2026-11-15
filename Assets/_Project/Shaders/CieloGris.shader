// Cielo de invierno cubierto: degradado de gris (arriba) a la niebla (horizonte), con nubes bajas muy suaves que se
// mueven despacio. WeatherSystem cambia los colores con la hora y el tiempo; el horizonte es siempre el color de la
// niebla, para que los edificios lejanos se fundan con el cielo.
Shader "GTAChiraq/CieloGris"
{
    Properties
    {
        _Top ("Arriba", Color) = (0.44, 0.47, 0.52, 1)
        _Horizon ("Horizonte", Color) = (0.56, 0.58, 0.62, 1)
        _Bottom ("Abajo", Color) = (0.48, 0.49, 0.53, 1)
        _Falloff ("Caída del degradado", Float) = 0.6
        _Clouds ("Contraste de las nubes", Range(0, 0.3)) = 0.08
        _CloudSpeed ("Velocidad de las nubes", Float) = 0.004
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Top, _Horizon, _Bottom;
                float _Falloff, _Clouds, _CloudSpeed;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.dir = input.positionOS.xyz;
                return o;
            }

            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), u.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), u.x), u.y);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float up = pow(saturate(d.y), _Falloff);
                half3 c = lerp(_Horizon.rgb, _Top.rgb, up);

                // Nubes: ruido proyectado sobre un techo plano, más tenue hacia el horizonte.
                float2 uv = d.xz / (d.y + 0.25) * 1.6 + _Time.y * _CloudSpeed * float2(1.0, 0.4);
                float n = Noise(uv) * 0.6 + Noise(uv * 2.3) * 0.3 + Noise(uv * 5.1) * 0.1;
                c *= 1.0 + (n - 0.5) * 2.0 * _Clouds * saturate(d.y * 3.0);

                c = lerp(c, _Bottom.rgb, saturate(-d.y * 4.0));
                return half4(c, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}

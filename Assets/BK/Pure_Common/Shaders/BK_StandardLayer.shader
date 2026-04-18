// WebGL-Optimized version of BK/StandardLayered
// Changes from original:
//   - Removed _DetailAlbedo, _DetailNormalMap, _DetailMetallicGlossMap (3 samplers saved)
//   - Removed _LayerMetallicGlossMap (1 sampler saved)
//   - Layer maps now use flat world-space UV instead of triplanar (9x fewer samples)
//   - All textures share a single SamplerState (sampler register pressure: 9 -> 1)
//   - Shader target kept at 3.5 for WebGL2 compatibility
//   Total artist samplers: 5 (was 9), leaving ~11 free for URP internals

Shader "BK/StandardLayered_WebGL"
{
    Properties
    {
        [HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
        [Header(Mesh Maps)][Space(10)] _Color( "Main Color", Color ) = ( 1, 1, 1, 1 )
        _MainTex( "Albedo", 2D ) = "white" {}
        _BumpMap( "Normal", 2D ) = "bump" {}
        _NormalPower( "Normal Power", Range( 0, 1 ) ) = 1
        _MetallicGlossMap( "Metallic (R) Occlusion (G) Smoothness (A)", 2D ) = "black" {}
        _MetallicPower( "Metallic Power", Range( 0, 1 ) ) = 0
        _SmoothnessPower( "Smoothness Power", Range( 0, 1 ) ) = 0
        _OcclusionPower( "Occlusion Power", Range( 0, 1 ) ) = 0

        [Space(10)][Header(Layer Maps)][Space(10)]
        _LayerAlbedo( "Layer Albedo", 2D ) = "white" {}
        _LayerNormalMap( "Layer Normal", 2D ) = "bump" {}
        _SecondNormalPower( "Layer Normal Power", Range( 0, 1 ) ) = 1
        _Tiling2( "Layer Tiling", Float ) = 1

        [Space(10)][Header(Layer Blend)][Space(10)]
        _2ndColor( "Layer Color", Color ) = ( 1, 1, 1, 1 )
        [Toggle] _UseVertexColor( "Use Vertex Color", Float ) = 0
        [KeywordEnum( R,G,B,A )] _VertexColorChannel( "Vertex Color Channel", Float ) = 2
        [Space(10)]
        _LayerPower( "Layer Power", Range( 0, 1 ) ) = 0.5
        _LayerThreshold( "Layer Threshold", Range( 0, 50 ) ) = 50
        _LayerPosition( "Layer Position", Float ) = 0
        _LayerContrast( "Layer Contrast", Float ) = 0

        [ToggleOff(_SPECULARHIGHLIGHTS_OFF)] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 1.0

        [HideInInspector] _texcoord( "", 2D ) = "white" {}
        [HideInInspector] _QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector] _QueueControl("_QueueControl", Float) = -1
        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }

    SubShader
    {
        LOD 0

        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" "UniversalMaterialType"="Lit" }

        Cull Back
        ZWrite On
        ZTest LEqual
        Offset 0 , 0
        AlphaToMask Off

        HLSLINCLUDE
        #pragma target 3.5
        #pragma prefer_hlslcc gles

        #if ( SHADER_TARGET > 35 ) && defined( SHADER_API_GLES3 )
            #error For WebGL2/GLES3, please set your shader target to 3.5 via SubShader options.
        #endif

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
        ENDHLSL

        // -----------------------------------------------------------------------
        // FORWARD PASS
        // -----------------------------------------------------------------------
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForwardOnly" }

            Blend One Zero, One Zero
            ZWrite On
            ZTest LEqual
            Offset 0 , 0
            ColorMask RGBA

            HLSLPROGRAM

            #define _ALPHATEST_ON
            #define _NORMAL_DROPOFF_TS 1
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #define ASE_FOG 1
            #define _NORMALMAP 1

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS

            #pragma vertex vert
            #pragma fragment frag

            #define SHADERPASS SHADERPASS_FORWARD

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

            #if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

            #pragma shader_feature_local _VERTEXCOLORCHANNEL_R _VERTEXCOLORCHANNEL_G _VERTEXCOLORCHANNEL_B _VERTEXCOLORCHANNEL_A

            // ------------------------------------------------------------------
            // CBUFFER
            // ------------------------------------------------------------------
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float4 _MainTex_ST;
            float4 _BumpMap_ST;
            float4 _MetallicGlossMap_ST;
            float4 _LayerAlbedo_ST;
            float4 _LayerNormalMap_ST;
            float4 _2ndColor;
            float _NormalPower;
            float _SecondNormalPower;
            float _MetallicPower;
            float _SmoothnessPower;
            float _OcclusionPower;
            float _LayerPower;
            float _LayerThreshold;
            float _LayerPosition;
            float _LayerContrast;
            float _UseVertexColor;
            float _Tiling2;
            CBUFFER_END

            // ------------------------------------------------------------------
            // TEXTURES — declared with TEXTURE2D macro so they share one sampler
            // This is the key fix: 5 TEXTURE2D slots but only 1 sampler register
            // ------------------------------------------------------------------
            TEXTURE2D(_MainTex);
            TEXTURE2D(_BumpMap);
            TEXTURE2D(_MetallicGlossMap);
            TEXTURE2D(_LayerAlbedo);
            TEXTURE2D(_LayerNormalMap);
            // Single shared sampler state for all textures (saves 4 sampler registers)
            SAMPLER(sampler_MainTex);

            // ------------------------------------------------------------------
            // HELPERS
            // ------------------------------------------------------------------
            float4 CalculateContrast(float contrastValue, float4 colorTarget)
            {
                float t = 0.5 * (1.0 - contrastValue);
                return mul(float4x4(contrastValue,0,0,t, 0,contrastValue,0,t, 0,0,contrastValue,t, 0,0,0,1), colorTarget);
            }

            // ------------------------------------------------------------------
            // STRUCTS
            // ------------------------------------------------------------------
            struct Attributes
            {
                float4 positionOS   : POSITION;
                half3  normalOS     : NORMAL;
                half4  tangentOS    : TANGENT;
                float4 texcoord     : TEXCOORD0;
                float4 texcoord1    : TEXCOORD1; // always needed for OUTPUT_LIGHTMAP_UV
                #if defined(DYNAMICLIGHTMAP_ON)
                float4 texcoord2    : TEXCOORD2;
                #endif
                float4 ase_color    : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS               : SV_POSITION;
                float3 positionWS               : TEXCOORD0;
                half3  normalWS                 : TEXCOORD1;
                float4 tangentWS                : TEXCOORD2;
                float4 lightmapUVOrVertexSH     : TEXCOORD3;
                #if defined(ASE_FOG) || defined(_ADDITIONAL_LIGHTS_VERTEX)
                half4  fogFactorAndVertexLight  : TEXCOORD4;
                #endif
                #if defined(DYNAMICLIGHTMAP_ON)
                float2 dynamicLightmapUV        : TEXCOORD5;
                #endif
                #if defined(USE_APV_PROBE_OCCLUSION)
                float4 probeOcclusion           : TEXCOORD6;
                #endif
                float2 uv                       : TEXCOORD7;
                float4 vertexColor              : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ------------------------------------------------------------------
            // VERTEX
            // ------------------------------------------------------------------
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.uv          = input.texcoord.xy;
                output.vertexColor = input.ase_color;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                OUTPUT_LIGHTMAP_UV(input.texcoord1, unity_LightmapST, output.lightmapUVOrVertexSH.xy);
                #if defined(DYNAMICLIGHTMAP_ON)
                output.dynamicLightmapUV.xy = input.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
                #endif
                // OUTPUT_SH / OUTPUT_SH4 signature varies by URP version
                #if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
                    OUTPUT_SH4(vertexInput.positionWS, normalInput.normalWS.xyz,
                               GetWorldSpaceNormalizeViewDir(vertexInput.positionWS),
                               output.lightmapUVOrVertexSH.xyz, output.probeOcclusion);
                #else
                    OUTPUT_SH(normalInput.normalWS.xyz, output.lightmapUVOrVertexSH.xyz);
                #endif

                #if defined(ASE_FOG) || defined(_ADDITIONAL_LIGHTS_VERTEX)
                output.fogFactorAndVertexLight = 0;
                #if defined(ASE_FOG) && !defined(_FOG_FRAGMENT)
                output.fogFactorAndVertexLight.x = ComputeFogFactor(vertexInput.positionCS.z);
                #endif
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                output.fogFactorAndVertexLight.yzw = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
                #endif
                #endif

                output.positionCS  = vertexInput.positionCS;
                output.positionWS  = vertexInput.positionWS;
                output.normalWS    = normalInput.normalWS;
                output.tangentWS   = float4(normalInput.tangentWS, (input.tangentOS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale());

                return output;
            }

            // ------------------------------------------------------------------
            // FRAGMENT
            // ------------------------------------------------------------------
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                #if defined(LOD_FADE_CROSSFADE)
                LODFadeCrossFade(input.positionCS);
                #endif

                float renormFactor = 1.0 / max(FLT_MIN, length(input.normalWS));
                float3 PositionWS  = input.positionWS;
                float3 ViewDirWS   = GetWorldSpaceNormalizeViewDir(PositionWS);
                float3 TangentWS   = input.tangentWS.xyz * renormFactor;
                float3 NormalWS    = input.normalWS * renormFactor;
                float3 BitangentWS = cross(NormalWS, TangentWS) * input.tangentWS.w * renormFactor;
                float3x3 TBN       = float3x3(TangentWS, BitangentWS, NormalWS);

                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                float4 shadowCoord = TransformWorldToShadowCoord(PositionWS);
                #else
                float4 shadowCoord = float4(0,0,0,0);
                #endif

                // ---- UVs ------------------------------------------------
                float2 uvMain  = input.uv * _MainTex_ST.xy  + _MainTex_ST.zw;
                float2 uvBump  = input.uv * _BumpMap_ST.xy  + _BumpMap_ST.zw;
                float2 uvMOS   = input.uv * _MetallicGlossMap_ST.xy + _MetallicGlossMap_ST.zw;

                // Layer uses flat world-space top-down UV (replaces triplanar, 9x cheaper)
                float2 uvLayer = PositionWS.xz * _Tiling2;

                // ---- Sample textures (all share sampler_MainTex) ---------
                float4 mainAlbedo = SAMPLE_TEXTURE2D(_MainTex,      sampler_MainTex, uvMain);
                float4 mos        = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MainTex, uvMOS);
                float4 layerAlb   = SAMPLE_TEXTURE2D(_LayerAlbedo,  sampler_MainTex, uvLayer);
                float4 layerNrmSample = SAMPLE_TEXTURE2D(_LayerNormalMap, sampler_MainTex, uvLayer);

                // Base normal (tangent space)
                float3 baseNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_MainTex, uvBump), _NormalPower);
                baseNormal.z = lerp(1, baseNormal.z, saturate(_NormalPower));

                float3 layerNormal = UnpackNormalScale(layerNrmSample, _SecondNormalPower);
                layerNormal.z = lerp(1, layerNormal.z, saturate(_SecondNormalPower));

                // ---- Blend mask -----------------------------------------
                // Vertex color channel select
                #if defined(_VERTEXCOLORCHANNEL_R)
                float vcMask = input.vertexColor.r;
                #elif defined(_VERTEXCOLORCHANNEL_G)
                float vcMask = input.vertexColor.g;
                #elif defined(_VERTEXCOLORCHANNEL_B)
                float vcMask = input.vertexColor.b;
                #elif defined(_VERTEXCOLORCHANNEL_A)
                float vcMask = input.vertexColor.a;
                #else
                float vcMask = input.vertexColor.b;
                #endif

                // World-normal Y for slope-based blending (when not using vertex color)
                // We reconstruct world-normal from base tangent normal
                float3 tanToWorld0 = float3(TangentWS.x, BitangentWS.x, NormalWS.x);
                float3 tanToWorld1 = float3(TangentWS.y, BitangentWS.y, NormalWS.y);
                float3 tanToWorld2 = float3(TangentWS.z, BitangentWS.z, NormalWS.z);
                float3 worldNormalFromBase = float3(
                    dot(tanToWorld0, baseNormal),
                    dot(tanToWorld1, baseNormal),
                    dot(tanToWorld2, baseNormal));
                float slopeY = worldNormalFromBase.y;

                float saferPow = abs(vcMask);
                float4 vcPow   = (float4)(pow(saferPow, _LayerPosition));
                float4 clamped = clamp(CalculateContrast(_LayerContrast, vcPow), 0, float4(1,1,1,0));
                float  oneMinusPower = 1.0 - _LayerPower;
                float4 vcBlend = pow(clamped, (float4)(oneMinusPower)) * clamped;

                float4 blendSource = _UseVertexColor ? vcBlend : (float4)(slopeY);
                float  threshold   = 0.001 + (_LayerThreshold * (1.0 - 0.001));
                float4 BlendAlpha  = _2ndColor.a * pow(saturate(blendSource + _LayerPower), (float4)(threshold));

                // ---- Albedo ---------------------------------------------
                // Matches original shader logic:
                //   base  = _Color * (mainTex * layerAlbedo)   <- layer tiles over base mesh tex
                //   layer = _2ndColor * detailAlbedo           <- second material on blended areas
                // _LayerAlbedo was the "Second Maps" tiling overlay on the base mesh,
                // _DetailAlbedo was the standalone layer material. Since we dropped _DetailAlbedo
                // for WebGL, we use _LayerAlbedo for both roles: multiply it onto the base AND
                // use it as the blended layer color. This restores the green tint.
                float4 baseColor  = _Color * (mainAlbedo * layerAlb);
                float4 layerColor = _2ndColor * layerAlb;
                float4 Albedo     = lerp(baseColor, layerColor, BlendAlpha);

                // ---- Normal (blend base + layer) ------------------------
                float3 blendedNormal = BlendNormal(baseNormal, layerNormal);
                float3 Normal = lerp(baseNormal, blendedNormal, BlendAlpha.r);

                // ---- Metallic / Smoothness / Occlusion ------------------
                float metallic   = mos.r * _MetallicPower;
                float smoothness = mos.a * _SmoothnessPower;
                float saferOcc   = abs(mos.g);
                float occlusion  = pow(saferOcc, _OcclusionPower);

                // ---- Lighting setup -------------------------------------
                InputData inputData = (InputData)0;
                inputData.positionWS              = PositionWS;
                inputData.viewDirectionWS         = ViewDirWS;
                inputData.shadowCoord             = shadowCoord;
                inputData.normalWS                = NormalizeNormalPerPixel(
                    TransformTangentToWorld(Normal, half3x3(TangentWS, BitangentWS, NormalWS)));
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                #ifdef ASE_FOG
                inputData.fogCoord = InitializeInputDataFog(float4(PositionWS, 1.0), input.fogFactorAndVertexLight.x);
                #endif
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
                #endif

                float3 SH = input.lightmapUVOrVertexSH.xyz;
                #if defined(DYNAMICLIGHTMAP_ON)
                    inputData.bakedGI    = SAMPLE_GI(input.lightmapUVOrVertexSH.xy, input.dynamicLightmapUV.xy, SH, inputData.normalWS);
                    inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUVOrVertexSH.xy);
                #elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
                    // Adaptive Probe Volumes path (URP 14+)
                    inputData.bakedGI = SAMPLE_GI(
                        SH,
                        GetAbsolutePositionWS(inputData.positionWS),
                        inputData.normalWS,
                        inputData.viewDirectionWS,
                        input.positionCS.xy,
                        input.probeOcclusion,
                        inputData.shadowMask);
                #else
                    inputData.bakedGI    = SAMPLE_GI(input.lightmapUVOrVertexSH.xy, SH, inputData.normalWS);
                    inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUVOrVertexSH.xy);
                #endif

                SurfaceData surfaceData;
                surfaceData.albedo              = Albedo.rgb;
                surfaceData.metallic            = saturate(metallic);
                surfaceData.specular            = 0.5;
                surfaceData.smoothness          = saturate(smoothness);
                surfaceData.occlusion           = occlusion;
                surfaceData.emission            = 0;
                surfaceData.alpha               = 1;
                surfaceData.normalTS            = Normal;
                surfaceData.clearCoatMask       = 0;
                surfaceData.clearCoatSmoothness = 1;

                #if defined(_DBUFFER)
                ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
                #endif

                half4 color = UniversalFragmentPBR(inputData, surfaceData);

                #ifdef ASE_FOG
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                #endif

                return half4(color.rgb, OutputAlpha(color.a, false));
            }
            ENDHLSL
        }

        // -----------------------------------------------------------------------
        // SHADOW CASTER
        // -----------------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            AlphaToMask Off
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma prefer_hlslcc gles
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma vertex vert
            #pragma fragment frag

            #define SHADERPASS SHADERPASS_SHADOWCASTER

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            // ShaderGraphFunctions.hlsl excluded from ShadowCaster - not needed and
            // causes SampleSH errors when Lighting.hlsl include order differs.
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float4 _MainTex_ST;
            float4 _BumpMap_ST;
            float4 _MetallicGlossMap_ST;
            float4 _LayerAlbedo_ST;
            float4 _LayerNormalMap_ST;
            float4 _2ndColor;
            float _NormalPower;
            float _SecondNormalPower;
            float _MetallicPower;
            float _SmoothnessPower;
            float _OcclusionPower;
            float _LayerPower;
            float _LayerThreshold;
            float _LayerPosition;
            float _LayerContrast;
            float _UseVertexColor;
            float _Tiling2;
            CBUFFER_END

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                half3  normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldDir(input.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDir = normalize(_LightPosition - positionWS);
                #else
                float3 lightDir = _LightDirection;
                #endif
                float4 posCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDir));
                output.positionCS = ApplyShadowClamping(posCS);
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // -----------------------------------------------------------------------
        // DEPTH ONLY
        // -----------------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask R
            AlphaToMask Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma prefer_hlslcc gles
            #pragma multi_compile_instancing
            #pragma vertex vert
            #pragma fragment frag

            #define SHADERPASS SHADERPASS_DEPTHONLY

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // NOTE: ShaderGraphFunctions.hlsl intentionally excluded from DepthOnly -
            // it calls SampleSH which requires Lighting.hlsl and is not needed here.
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float4 _MainTex_ST;
            float4 _BumpMap_ST;
            float4 _MetallicGlossMap_ST;
            float4 _LayerAlbedo_ST;
            float4 _LayerNormalMap_ST;
            float4 _2ndColor;
            float _NormalPower;
            float _SecondNormalPower;
            float _MetallicPower;
            float _SmoothnessPower;
            float _OcclusionPower;
            float _LayerPower;
            float _LayerThreshold;
            float _LayerPosition;
            float _LayerContrast;
            float _UseVertexColor;
            float _Tiling2;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings   { float4 positionCS : SV_POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetVertexPositionInputs(input.positionOS.xyz).positionCS;
                return output;
            }
            half4 frag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }

        // -----------------------------------------------------------------------
        // META
        // -----------------------------------------------------------------------
        Pass
        {
            Name "Meta"
            Tags { "LightMode"="Meta" }
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma prefer_hlslcc gles
            #pragma shader_feature EDITOR_VISUALIZATION
            #pragma vertex vert
            #pragma fragment frag

            #define SHADERPASS SHADERPASS_META

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            // ShaderGraphFunctions excluded from Meta - MetaInput.hlsl is sufficient
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float4 _MainTex_ST;
            float4 _BumpMap_ST;
            float4 _MetallicGlossMap_ST;
            float4 _LayerAlbedo_ST;
            float4 _LayerNormalMap_ST;
            float4 _2ndColor;
            float _NormalPower;
            float _SecondNormalPower;
            float _MetallicPower;
            float _SmoothnessPower;
            float _OcclusionPower;
            float _LayerPower;
            float _LayerThreshold;
            float _LayerPosition;
            float _LayerContrast;
            float _UseVertexColor;
            float _Tiling2;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            TEXTURE2D(_LayerAlbedo);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 texcoord   : TEXCOORD0;
                float4 texcoord1  : TEXCOORD1;
                float4 texcoord2  : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                #ifdef EDITOR_VISUALIZATION
                float4 VizUV      : TEXCOORD2;
                float4 LightCoord : TEXCOORD3;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.uv = input.texcoord.xy;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                #ifdef EDITOR_VISUALIZATION
                float2 VizUV = 0; float4 LightCoord = 0;
                UnityEditorVizData(input.positionOS.xyz, input.texcoord.xy, input.texcoord1.xy, input.texcoord2.xy, VizUV, LightCoord);
                output.VizUV = float4(VizUV,0,0); output.LightCoord = LightCoord;
                #endif
                output.positionCS = MetaVertexPosition(input.positionOS, input.texcoord1.xy, input.texcoord1.xy, unity_LightmapST, unity_DynamicLightmapST);
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float2 uvMain  = input.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                float2 uvLayer = input.positionWS.xz * _Tiling2;
                float4 mainAlb = SAMPLE_TEXTURE2D(_MainTex,    sampler_MainTex, uvMain);
                float4 layerAlb= SAMPLE_TEXTURE2D(_LayerAlbedo,sampler_MainTex, uvLayer);
                // Simplified blend for meta pass (no vertex color available)
                float4 albedo  = lerp(_Color * mainAlb, _2ndColor * layerAlb, _LayerPower);
                MetaInput metaInput = (MetaInput)0;
                metaInput.Albedo = albedo.rgb;
                metaInput.Emission = 0;
                #ifdef EDITOR_VISUALIZATION
                metaInput.VizUV = input.VizUV.xy;
                metaInput.LightCoord = input.LightCoord;
                #endif
                return UnityMetaFragment(metaInput);
            }
            ENDHLSL
        }

        // -----------------------------------------------------------------------
        // DEPTH NORMALS
        // -----------------------------------------------------------------------
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }

            ZWrite On
            Blend One Zero
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma prefer_hlslcc gles
            #pragma multi_compile_instancing
            #pragma vertex vert
            #pragma fragment frag

            #define _NORMAL_DROPOFF_TS 1
            #define _NORMALMAP 1
            #define SHADERPASS SHADERPASS_DEPTHNORMALSONLY

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            // ShaderGraphFunctions.hlsl excluded - Lighting.hlsl already provides what we need
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float4 _MainTex_ST;
            float4 _BumpMap_ST;
            float4 _MetallicGlossMap_ST;
            float4 _LayerAlbedo_ST;
            float4 _LayerNormalMap_ST;
            float4 _2ndColor;
            float _NormalPower;
            float _SecondNormalPower;
            float _MetallicPower;
            float _SmoothnessPower;
            float _OcclusionPower;
            float _LayerPower;
            float _LayerThreshold;
            float _LayerPosition;
            float _LayerContrast;
            float _UseVertexColor;
            float _Tiling2;
            CBUFFER_END

            TEXTURE2D(_BumpMap);
            TEXTURE2D(_LayerNormalMap);
            SAMPLER(sampler_BumpMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                half3  normalOS   : NORMAL;
                half4  tangentOS  : TANGENT;
                float2 texcoord   : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3  normalWS   : TEXCOORD1;
                float4 tangentWS  : TEXCOORD2;
                float2 uv         : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.uv = input.texcoord;
                VertexPositionInputs vpi = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   vni = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS  = vpi.positionCS;
                output.positionWS  = vpi.positionWS;
                output.normalWS    = vni.normalWS;
                output.tangentWS   = float4(vni.tangentWS, (input.tangentOS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale());
                return output;
            }
            void frag(Varyings input, out half4 outNormalWS : SV_Target0)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float renormFactor = 1.0 / max(FLT_MIN, length(input.normalWS));
                float3 TangentWS   = input.tangentWS.xyz * renormFactor;
                float3 NormalWS    = input.normalWS * renormFactor;
                float3 BitangentWS = cross(NormalWS, TangentWS) * input.tangentWS.w * renormFactor;

                float2 uvBump  = input.uv * _BumpMap_ST.xy + _BumpMap_ST.zw;
                float2 uvLayer = input.positionWS.xz * _Tiling2;
                float3 baseN   = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,       sampler_BumpMap, uvBump),  _NormalPower);
                float3 layerN  = UnpackNormalScale(SAMPLE_TEXTURE2D(_LayerNormalMap, sampler_BumpMap, uvLayer), _SecondNormalPower);
                float3 blended = BlendNormal(baseN, layerN);
                float3 normalWS= NormalizeNormalPerPixel(TransformTangentToWorld(blended, half3x3(TangentWS, BitangentWS, NormalWS)));
                outNormalWS = half4(normalWS, 0.0);
            }
            ENDHLSL
        }
    }

    CustomEditor "UnityEditor.ShaderGraphLitGUI"
    FallBack "Hidden/Shader Graph/FallbackError"
    Fallback Off
}

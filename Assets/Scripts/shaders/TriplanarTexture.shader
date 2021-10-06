Shader "Custom/TriplanarTexture"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _NormalTex ("Normal Map", 2D) = "White" {} 
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _TextureScale ("Texture Scale", Float) = 10
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.5

        sampler2D _MainTex;
        sampler2D _NormalTex;

        struct Input
        {
			float3 vertPos;
			float3 normal;
			float3 worldNormal;
			float4 tangent;
			float3 viewDir;
        };

        half _Metallic;
        fixed4 _Color;
        float _TextureScale;


        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        float4 triplanar(float3 vertPos, float3 normal, float scale, sampler2D diffuseTex) {

            float2 uvX = vertPos.zy * scale;
            float2 uvY = vertPos.xz * scale;
            float2 uvZ = vertPos.xy * scale;

            float4 diffuseX = tex2D (diffuseTex, uvX);
            float4 diffuseY = tex2D (diffuseTex, uvY);
            float4 diffuseZ = tex2D (diffuseTex, uvZ);

            float3 blendWeight = normal * normal;
            blendWeight /= dot(blendWeight, 1);
            return diffuseX * blendWeight.x + diffuseY * blendWeight.y + diffuseZ * blendWeight.z;
        }

        float3 ObjectToTangentVector(float4 tangent, float3 normal, float3 objectSpaceVector) {
            float3 normalizedTangent = normalize(tangent.xyz);
            float3 binormal = cross(normal, normalizedTangent) * tangent.w;
            float3x3 rot = float3x3 (normalizedTangent, binormal, normal);
            return mul(rot, objectSpaceVector);
        }

        float3 rnmBlendUnpacked(float3 n1, float3 n2)
        {
            n1 += float3( 0,  0, 1);
            n2.xy = -n2.xy;

            return n1 * dot(n1, n2) / n1.z - n2;
        }

        float3 triplanarNormal(float3 vertPos, float3 normal, float3 scale, float2 offset, float4 tangent, sampler2D normalMap) {
            float3 absNormal = abs(normal);

            // Calculate triplanar blend
            float3 blendWeight = saturate(pow(normal, 4));
            blendWeight /= dot(blendWeight, 1);

            float2 uvX = vertPos.zy * scale;
            float2 uvY = vertPos.xz * scale;
            float2 uvZ = vertPos.xy * scale;

            float3 tangentNormalX = UnpackNormal(tex2D(normalMap, uvX));
            float3 tangentNormalY = UnpackNormal(tex2D(normalMap, uvY));
            float3 tangentNormalZ = UnpackNormal(tex2D(normalMap, uvZ));

            // TODO: Blend with vertex normals using RNM https://blog.selfshadow.com/publications/blending-in-detail/
            tangentNormalX = rnmBlendUnpacked(half3(normal.zy, absNormal.x), tangentNormalX);
            tangentNormalY = rnmBlendUnpacked(half3(normal.xz, absNormal.y), tangentNormalY);
            tangentNormalZ = rnmBlendUnpacked(half3(normal.xy, absNormal.z), tangentNormalZ);

            float3 axisSign = sign(normal);
            tangentNormalX.z *= axisSign.x;
            tangentNormalY.z *= axisSign.y;
            tangentNormalZ.z *= axisSign.z;


            // TODO: How does this swizzle work
            float3 worldNormal = normalize(
                tangentNormalX.zyx * blendWeight.x +
                tangentNormalY.xzy * blendWeight.y +
                tangentNormalZ.xyz * blendWeight.z
            );

            return ObjectToTangentVector(tangent, normal, worldNormal);

        }

        void vert (inout appdata_full v, out Input o) {

            UNITY_INITIALIZE_OUTPUT (Input,o);

            o.vertPos = v.vertex;
			o.normal = v.normal;
			o.tangent = v.tangent;
            float3 normWorld = normalize(mul(unity_ObjectToWorld, float4(v.normal,0)));
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            float3 viewDir = normalize(worldPos - _WorldSpaceCameraPos.xyz);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 diffuse = triplanar(IN.vertPos, IN.normal, _TextureScale, _MainTex);
            float3 normal = triplanarNormal(IN.vertPos, IN.normal, _TextureScale, 0, IN.tangent, _NormalTex);
            o.Normal = normal;
            o.Albedo = diffuse * _Color;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Alpha = diffuse.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

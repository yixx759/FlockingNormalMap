#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"


struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float3 normal : TEXCOORD1;
    float3 tan : TEXCOORD5;
    float3 bivec : TEXCOORD6;
    float3 wpos : TEXCOORD2;
    LIGHTING_COORDS(3,4)

    float4 vertex : SV_POSITION;
};

sampler2D _MainTex;
float4 _MainTex_ST;
float4 _HeightMap_ST;
sampler2D _HeightMap;
float4 _HeightMap_TexelSize;
float _Gloss;


v2f vert (appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    
    o.wpos = mul(unity_ObjectToWorld, v.vertex);
   o.uv = TRANSFORM_TEX( v.uv,_HeightMap);
   // o.normal =  UnityObjectToWorldNormal(v.normal);
 
    o.normal =  UnityObjectToWorldNormal(v.normal);
    o.tan =  UnityObjectToWorldDir(v.tangent.xyz);
    o.bivec = cross(o.normal, o.tan);
    o.bivec *= v.tangent.w * unity_WorldTransformParams.w;
    TRANSFER_VERTEX_TO_FRAGMENT(o)
    
    return o;
}

fixed4 frag (v2f i) : SV_Target
{
 
   

             
    fixed3 norm = tex2D(_HeightMap, i.uv);
 

    float3x3 basematrix = {i.tan.x, i.normal.x, i.bivec.x,
        i.tan.y, i.normal.y, i.bivec.y,
    i.tan.z, i.normal.z, i.bivec.z};


    
   float3 N = mul(basematrix, norm);
  
  
    
  
N = normalize(N);


    
    float3 L = normalize(UnityWorldSpaceLightDir(i.wpos));
    float atten = LIGHT_ATTENUATION(i);


     float3 lambert = saturate(dot(N,L));
    float3 collambert = (lambert * atten)* _LightColor0.xyz;
    float3 V = normalize(_WorldSpaceCameraPos-i.wpos);
    float3 H = normalize(L + V);

    float3 spec = saturate(dot(H, N)) * (lambert > 0);
    float specex = exp2(_Gloss* 11)+2;
    spec = pow(spec, specex) * atten * _Gloss;
    spec *= _LightColor0;

    
  
    
   
    
   

  
    
    //float4 trucol = float4(col.xyz, w)*w;
    float4 trucol = tex2D(_MainTex, i.uv);
   


    //float lum = dot(trucol, float3( 0.2125f, 0.7152,  0.0724 ));

    
    

 
    return float4(( trucol * collambert.xyz+spec).xyz,1);
    }
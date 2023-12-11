Shader "Unlit/HeightoNorm"
{
   Properties
    {
        _MainTex ("col", 2D) = "white" {}
        _Gloss ("gloss", float) = 1
        _HeightMap ("Normal", 2D)= "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Tags{ "LightMode" = "ForwardBase"}
            CGPROGRAM
          
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
           #include "LightCG11.cginc"
            #include "AutoLight.cginc"
          
            ENDCG
        }
        Pass
        {
            Tags{ "LightMode" = "ForwardAdd"}
            Blend One One
            CGPROGRAM
             #pragma multi_compile_fwdadd
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #include "LightCG11.cginc"
            #include "AutoLight.cginc"
          
            ENDCG
        }
        
    }
    
     Fallback "VertexLit"
}

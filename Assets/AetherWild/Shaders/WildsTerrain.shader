Shader "AetherWild/WildsTerrain"
{
    Properties { _MainTex ("Original map",2D)="white" {} _Mask("Solid",2D)="white" {} _Initial("Original solid",2D)="white" {} _SourceHeight("Art boundary",2D)="white" {} _BackgroundOnly("Scenic layer",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex,_Mask,_Initial,_SourceHeight;
            float4 _Grid;
            float _BackgroundOnly;
            struct app { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct vary { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            vary vert(app v) { vary o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o; }
            fixed4 frag(vary i):SV_Target
            {
                if(_BackgroundOnly>.5) return tex2D(_MainTex,i.uv);
                float2 mu=(i.uv*_Grid.xy+.5)/(_Grid.xy+1);
                float solid=tex2D(_Mask,mu).r;
                float old=tex2D(_Initial,mu).r;
                fixed4 col=tex2D(_MainTex,i.uv);
                // Empty gameplay cells reveal the separate scenic backdrop, including the sky.
                // Original terrain texture remains unchanged wherever terrain is still solid.
                if(solid<.5) return fixed4(0,0,0,0);
                if(solid>.5 && (old<.5 || i.uv.y>tex2D(_SourceHeight,float2(i.uv.x,.5)).r))
                    col=tex2D(_MainTex,float2(i.uv.x,frac(i.uv.y)*.28)) * fixed4(.9,.95,.9,1);
                // A thin surface guide distinguishes the simplified playable profile from decoration.
                float above=tex2D(_Mask,mu+float2(0,.45/(_Grid.y+1))).r;
                if(solid>=.5 && above<.5) col=lerp(col,fixed4(.55,.62,.33,1),.65);
                return col;
            }
            ENDCG
        }
    }
}

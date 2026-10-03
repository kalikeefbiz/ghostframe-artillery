Shader "AetherWild/WildsTerrain"
{
    Properties { _MainTex ("Original map",2D)="white" {} _Mask("Solid",2D)="white" {} _Initial("Original solid",2D)="white" {} _SourceHeight("Art boundary",2D)="white" {} }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex,_Mask,_Initial,_SourceHeight;
            float4 _Grid;
            struct app { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct vary { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            vary vert(app v) { vary o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o; }
            fixed4 frag(vary i):SV_Target
            {
                float2 mu=(i.uv*_Grid.xy+.5)/(_Grid.xy+1);
                float solid=tex2D(_Mask,mu).r;
                float old=tex2D(_Initial,mu).r;
                fixed4 col=tex2D(_MainTex,i.uv);
                // Removed cells read as dark excavated cavities. Added walls use source rock texture.
                if(old>.5 && solid<.5) col=fixed4(.045,.065,.075,1);
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

// Pcx - Point cloud importer & renderer for Unity
// https://github.com/keijiro/Pcx

#include "UnityCG.cginc"
#include "Common.cginc"

// Uniforms
half4 _Tint;
half _PointSizeScale;
float _PointSize;
float4x4 _Transform;
//float4x4 _InverseProjMatrix;

#if _COMPUTE_BUFFER
StructuredBuffer<float4> _PointBuffer;
#endif

int _UseInterpolation;
int _UseAdaptivePointSize;




// Vertex input attributes
struct Attributes
{
	float4 uv: TEXCOORD1;
#if _COMPUTE_BUFFER
    uint vertexID : SV_VertexID;
#else
    float4 position : POSITION;
    half3 color : COLOR;
#endif
};

// Fragment varyings
struct Varyings
{
	//float4 position : SV_POSITION;
    float4 viewposition : TEXCOORD2;
	float4 uv: TEXCOORD1;
#if !PCX_SHADOW_CASTER
    half3 color : COLOR;
#endif
};



struct VertOut
{
	float4 position : SV_POSITION;
	float4 viewposition: TEXCOORD2;
	float2 uv : TEXCOORD0;
	float size : POINTSIZE;
#if !PCX_SHADOW_CASTER
	half3 color : COLOR;
#endif
};

struct FragOut {
	half4 color : SV_Target;
	float depth : SV_Depth;
};




// Vertex phase
Varyings Vertex(Attributes input)
{
    // Retrieve vertex attributes.
	half3 col;

#if _COMPUTE_BUFFER
	float4 pt = _PointBuffer[input.vertexID];
	float4 pos = mul(_Transform, float4(pt.xyz, 1));
	col = PcxDecodeColor(asuint(pt.w));
#else
	float4 pos = input.position;
	col = input.color;
#endif


#if !PCX_SHADOW_CASTER
    // Color space convertion & applying tint
    #if UNITY_COLORSPACE_GAMMA
        col *= _Tint.rgb * 2;
    #else
        col *= LinearToGammaSpace(_Tint.rgb) * 2;
        col = GammaToLinearSpace(col);
    #endif
#endif

    // Set vertex output.
    Varyings o;
    o.viewposition = mul(UNITY_MATRIX_MV, pos);
	o.uv = input.uv;
#if !PCX_SHADOW_CASTER
	o.color = col;
#endif
    return o;
}

// Geometry phase
[maxvertexcount(4)]
void Geometry(point Varyings input[1], inout TriangleStream<VertOut> outStream)
{
	float size;
	if (input[0].uv.x == 0 || _UseAdaptivePointSize < 0.5) {
		size = 0.001;
	}
	else {
		size = input[0].uv.x;
	}

    float4 origin = input[0].viewposition;
	float extent = size * _PointSize * _PointSizeScale;

    Varyings o = input[0];
	VertOut vo;
	vo.size = size;
#if !PCX_SHADOW_CASTER
	vo.color = o.color;
	//UNITY_TRANSFER_FOG(vo, o.position);
#endif

    vo.viewposition.y = origin.y + extent;
	vo.viewposition.x = origin.x - extent;
    vo.viewposition.zw = origin.zw;
	vo.position = mul(UNITY_MATRIX_P, vo.viewposition);
	vo.uv = float2(-1.0f, 1.0f);
    outStream.Append(vo);

	vo.viewposition.y = origin.y + extent;
	vo.viewposition.x = origin.x + extent;
	vo.viewposition.zw = origin.zw;
	vo.position = mul(UNITY_MATRIX_P, vo.viewposition);
	vo.uv = float2(1.0f, 1.0f);
	outStream.Append(vo);

	vo.viewposition.y = origin.y - extent;
	vo.viewposition.x = origin.x - extent;
	vo.viewposition.zw = origin.zw;
	vo.position = mul(UNITY_MATRIX_P, vo.viewposition);
	vo.uv = float2(-1.0f, -1.0f);
	outStream.Append(vo);

	vo.viewposition.y = origin.y - extent;
	vo.viewposition.x = origin.x + extent;
	vo.viewposition.zw = origin.zw;;
	vo.position = mul(UNITY_MATRIX_P, vo.viewposition);
	vo.uv = float2(1.0f, -1.0f);
	outStream.Append(vo);

    //outStream.RestartStrip();
}



FragOut Fragment(VertOut input)
{
	FragOut o;
	half4 c;
#if PCX_SHADOW_CASTER
	c = 0;
#else
    c = half4(input.color, _Tint.a);
	//UNITY_APPLY_FOG(input.fogCoord, c);
#endif
	

	if (_UseInterpolation > 0.5) {
		float uvlen = input.uv.x*input.uv.x + input.uv.y*input.uv.y;
		input.viewposition.z += (1 - uvlen) * input.size * _PointSize * _PointSizeScale;
	}
	
	
	float4 pos = mul(UNITY_MATRIX_P, input.viewposition);
	pos /= pos.w;
	//float4 pos = input.position;

	o.color = c;
	o.depth = pos.z;
	return o;
}



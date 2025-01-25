// XOutlineAAEdgeInfo.hlsl

bool XIsOutlinePixel(float4 gbuffer)
{
    return gbuffer.z != 0 || gbuffer.w != 0;
}

// Define the function to add edge information for the outline
void XOutlineAAEdgeInfo_float(
    float4 center_gbuffer, float4 left_gbuffer, float4 right_gbuffer, float4 down_gbuffer, float4 up_gbuffer,
    float4 center_color, float4 left_color, float4 right_color, float4 down_color, float4 up_color,
    out bool is_edge, out float4 non_outline_color, out float4 outline_color, out float4 outline_gbuffer)
{
    // if in this 5 pixel cross, there are both outline and non-outline pixels, then it is an edge
    bool center_is_outline = XIsOutlinePixel(center_gbuffer);
    bool left_is_outline = XIsOutlinePixel(left_gbuffer);
    bool right_is_outline = XIsOutlinePixel(right_gbuffer);
    bool down_is_outline = XIsOutlinePixel(down_gbuffer);
    bool up_is_outline = XIsOutlinePixel(up_gbuffer);

    is_edge = (center_is_outline && (!left_is_outline || !right_is_outline || !down_is_outline || !up_is_outline)) ||
            (!center_is_outline && (left_is_outline || right_is_outline || down_is_outline || up_is_outline));

    if (is_edge)
    {
        outline_gbuffer = 
                    center_is_outline ? center_gbuffer :
                    left_is_outline ? left_gbuffer :
                    right_is_outline ? right_gbuffer :
                    down_is_outline ? down_gbuffer :
                    up_is_outline ? up_gbuffer : float4(0, 0, 0, 0);

        outline_color = 
                    center_is_outline ? center_color :
                    left_is_outline ? left_color :
                    right_is_outline ? right_color :
                    down_is_outline ? down_color :
                    up_is_outline ? up_color : float4(0, 0, 0, 0);

        non_outline_color = 
                    !center_is_outline ? center_color :
                    !left_is_outline ? left_color :
                    !right_is_outline ? right_color :
                    !down_is_outline ? down_color :
                    !up_is_outline ? up_color : center_color;
    }
    else
    {
        outline_gbuffer = float4(0, 0, 0, 0); // or some other default value
        outline_color = float4(0, 0, 0, 0); // or some other default value
        non_outline_color = center_color;
    }
}


void XOutlineAAEdgeInfo_half(
    float4 center_gbuffer, float4 left_gbuffer, float4 right_gbuffer, float4 down_gbuffer, float4 up_gbuffer,
    float4 center_color, float4 left_color, float4 right_color, float4 down_color, float4 up_color,
    out bool is_edge, out half4 non_outline_color, out half4 outline_color, out half4 outline_gbuffer)
{
    // if in this 5 pixel cross, there are both outline and non-outline pixels, then it is an edge
    bool center_is_outline = XIsOutlinePixel(center_gbuffer);
    bool left_is_outline = XIsOutlinePixel(left_gbuffer);
    bool right_is_outline = XIsOutlinePixel(right_gbuffer);
    bool down_is_outline = XIsOutlinePixel(down_gbuffer);
    bool up_is_outline = XIsOutlinePixel(up_gbuffer);

    is_edge = (center_is_outline && (!left_is_outline || !right_is_outline || !down_is_outline || !up_is_outline)) ||
            (!center_is_outline && (left_is_outline || right_is_outline || down_is_outline || up_is_outline));

    if (is_edge)
    {
        outline_gbuffer = 
                    center_is_outline ? center_gbuffer :
                    left_is_outline ? left_gbuffer :
                    right_is_outline ? right_gbuffer :
                    down_is_outline ? down_gbuffer :
                    up_is_outline ? up_gbuffer : half4(0, 0, 0, 0);

        outline_color = 
                    center_is_outline ? center_color :
                    left_is_outline ? left_color :
                    right_is_outline ? right_color :
                    down_is_outline ? down_color :
                    up_is_outline ? up_color : half4(0, 0, 0, 0);

        non_outline_color = 
                    !center_is_outline ? center_color :
                    !left_is_outline ? left_color :
                    !right_is_outline ? right_color :
                    !down_is_outline ? down_color :
                    !up_is_outline ? up_color : center_color;
    }
    else
    {
        outline_gbuffer = half4(0, 0, 0, 0); // or some other default value
        outline_color = half4(0, 0, 0, 0); // or some other default value
        non_outline_color = center_color;
    }
}
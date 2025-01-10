using UnityEngine;
using UnityEngine.Events;

[ExecuteInEditMode]
[AddComponentMenu("XConstraint/2. Constraints (physical)/Collision")]
public class XCollisionConstraint : XConstraintWithSingleSource
{
    [Header("Params")]
    public LayerMask collisionMask;    // 碰撞检测的层
    public float radius = 0.5f;        // 碰撞检测的半径
    public Vector3 offset;             // 碰撞体的偏移量（局部空间）
	[Min(1)]
	public int forwardSteps = 3;      
	// public int backwardSteps = 2;

    private Vector3 prevPos;           // 记录上一帧的位置，用于确定如果发生碰撞向哪里退
	private bool isPrevPosInited = false;

	[Header("Callback")]

	public UnityEvent onCollision;

	public override void Resolve()
	{
		if (!isPrevPosInited)
		{
			prevPos = Source.position;
			isPrevPosInited = true;
		}

		// 从上次的位置逐步搜索到目标位置

		var targetPos = Source.position;
		var currentPos = prevPos;

        // 偏移影响下的实际碰撞检测位置
        var targetCheckPos = Source.TransformPoint(offset);
		var currentCheckPos = currentPos + targetCheckPos - targetPos;

        // 
        var delta = targetPos - currentPos;
        var step = delta / forwardSteps;

		bool collided = false;

        for (int i = 0; i < forwardSteps; i++)
        {
			if (Physics.CheckSphere(currentCheckPos + step, radius, collisionMask))
			{
				collided = true;
				break; 
			}

			currentPos += step;
			currentCheckPos += step;
        }

		if (collided)
			onCollision.Invoke();

        // 利用 Lerp 插值，应用 _influence
        Source.position = Vector3.Lerp(Source.position, currentPos, Influence);

        prevPos = Source.position;
    }

	/*
    public override void Execute(float upperLayerInfluence)
    {
        var _influence = influence * upperLayerInfluence;
        var currentPos = Source.position;

        // 偏移影响下的实际碰撞检测位置
        var checkPos = Source.TransformPoint(offset);

        // 计算当前帧与上一帧的位置差异
        var delta = currentPos - prevPos;
        var back = -delta.normalized;
        var step = delta.magnitude / maxIterations;

		bool collided = false;

        // 碰撞检测与循环合并
        for (int i = 0; i <= maxIterations; i++)
        {
            // 如果没有碰撞，跳出循环
            if (!Physics.CheckSphere(checkPos, radius, collisionMask))
            {
                break;
            }

            // 否则就是碰撞了，那么回退固定步长

			collided = true;

            checkPos += back * step;
            currentPos += back * step;  // 同时更新 currentPos
        }

		if (collided)
			onCollision.Invoke();

        // 利用 Lerp 插值，应用 _influence
        Source.position = Vector3.Lerp(Source.position, currentPos, _influence);

        prevPos = Source.position;
    }
	*/

    private void OnDrawGizmos()
    {
        if (Source == null) return;

        Gizmos.color = Color.red;

        // 在局部空间中应用偏移并转换到世界空间以显示 Gizmo
        var offsetWorld = Source.TransformPoint(offset);
        Gizmos.DrawWireSphere(offsetWorld, radius);
    }
}
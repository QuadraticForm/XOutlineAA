using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("XConstraint/2. Constraints (physical)/Spring Follow")]
public class XSpringFollowConstraint : XConstraintWithSingleSourceAndSingleTarget
{
#if UNITY_EDITOR
	[XQuickButton(new[] { "SetSourceAsTarget", "SnapSourceToTarget" }, new[] { "👉Follow Self👈", "🧲 Snap To Target" })]
	public bool _buttonDummy1 = false;

	public void SetSourceAsTarget()
	{
		 target = source;
	}

	public void SnapSourceToTarget()
	{
		if (target == null) return;

		Source.position = target.position;
		Source.rotation = target.rotation;
		spp = Source.position;
		spv = new Vector3(0,0,0);
		tpp = target.position;
	}
#endif

	

	[Header("Spring Settings")]
	[Range(0,1)]
	public float springness = 1.0f;
	[Min(0)]
    public float stiffness = 500.0f; // 弹簧刚度系数
	[Min(0)]
    public float damping = 20.0f; // 阻尼系数

	public enum Method
	{
		simple,
		analytical
	}

	public Method method = Method.analytical;

	public XVector3Bool axis = new XVector3Bool(true);

	[Header("Debug")]
	public bool _debug = false;
	[XConditionalHide("_debug", true, XConditionalHideAttribute.CompareMethod.NotEqual)]
    public Vector3 spp = new Vector3(0,0,0); // spv is Source Previous position
	[XConditionalHide("_debug", true, XConditionalHideAttribute.CompareMethod.NotEqual)]
    public Vector3 spv = new Vector3(0,0,0); // spv is Source Previous Velocity
	[XConditionalHide("_debug", true, XConditionalHideAttribute.CompareMethod.NotEqual)]
	public Vector3 tpp = new Vector3(0,0,0); // tpp is Target Previous Position

	private bool isPrevStatesInited = false;

	private void Start()
	{
		if (target != null)
        {
            tpp = target.position;
        }
	}

	float TimeStep { get { return Mathf.Max(Time.smoothDeltaTime, 1 / 240.0f); } }

	public override void Resolve()
	{
		if (!isPrevStatesInited)
		{
			spp = Source.position;
			spv = Vector3.zero;
			tpp = target.position;
			isPrevStatesInited = true;
		}

		var scp = new Vector3();					// source current pos
		var scv = new Vector3();					// source current velocity
		// var scv = (scp - spp) / Time.deltaTime;		// source current velocity

		var tcp = target.position;					// target current pos
		var tcv = XMath.FallbackIfNotValid((tcp - tpp) / TimeStep);		// target current velocity

		if (method == Method.simple)
			SimpleDampedSpring(spp, spv, tpp, tcv, stiffness, damping, TimeStep, out scp, out scv);
		else
			AnalyticalDampedSpring(spp, spv, tpp, tcv, stiffness, damping, TimeStep, out scp, out scv);

		// fail safe
		scp = XMath.FallbackIfNotValid(scp, spp);
		scv = XMath.FallbackIfNotValid(scv, spv);

		// record prev states BEFORE springness and influence is tested to be more smooth
		spv = XMath.FallbackIfNotValid((scp - spp) / TimeStep, spv); // fail safe
		spp = scp;
		tpp = tcp;

		// springness

		scp = Vector3.Lerp(tcp, scp, springness);
		
		// influence
		scp = Vector3.Lerp(source.position, scp, Influence);
		Source.position = scp;

		// axis mask

		if (axis != XVector3Bool.AllTrue)
		{
			var posInLocalSpace = sourceRest.parentToLocal.MultiplyPoint(source.GetPositionInParentSpace());

			posInLocalSpace = XConstraintsUtil.MaskChannels(posInLocalSpace, Vector3.zero, axis);

			Source.SetPositionInParentSpace(sourceRest.localToParent.MultiplyPoint(posInLocalSpace));
		}

		// record prev original
		// spv = XMath.FallbackIfNotValid((scp - spp) / TimeStep, spv); // fail safe
		// spp = scp;
		// tpp = tcp;
	}


	public static void SimpleDampedSpring(
        Vector3 initialPositionSource,
        Vector3 initialVelocitySource,
        Vector3 initialPositionTarget,
        Vector3 constantVelocityTarget,
        float springConstant,
        float dampingCoefficient,
        float time,
        out Vector3 positionSource,
        out Vector3 velocitySource)
    {
        // Calculate the relative initial position and velocity
        Vector3 relativeInitialPosition = initialPositionSource - initialPositionTarget;
        Vector3 relativeInitialVelocity = initialVelocitySource - constantVelocityTarget;

        // Calculate the spring force acting on the source
        Vector3 springForce = -springConstant * relativeInitialPosition;

        // Calculate the damping force acting on the source
        Vector3 dampingForce = -dampingCoefficient * relativeInitialVelocity;

        // Calculate the total force acting on the source
        Vector3 totalForce = springForce + dampingForce;

        // Assuming mass m=1 for simplicity, acceleration due to total force is equal to the total force
        Vector3 accelerationSource = totalForce;

        // Update the velocity of the source considering the total force
        velocitySource = initialVelocitySource + accelerationSource * time;

        // Update the position of the source considering initial velocity and acceleration due to total force
        positionSource = initialPositionSource + initialVelocitySource * time + 0.5f * accelerationSource * time * time;
    }


	public static void AnalyticalDampedSpring(
        Vector3 initialPositionSource,
        Vector3 initialVelocitySource,
        Vector3 initialPositionTarget,
        Vector3 constantVelocityTarget,
        float springConstant,
        float dampingCoefficient,
        float time,
        out Vector3 positionSource,
        out Vector3 velocitySource)
    {
        // Constants for the spring-damper system
        float m = 1f; // Assume unit mass for simplicity
        float omega = Mathf.Sqrt(springConstant / m);
        float gamma = dampingCoefficient / (2 * m);

        // Initial relative position and velocity
        Vector3 relativeInitialPosition = initialPositionSource - initialPositionTarget;
        Vector3 relativeInitialVelocity = initialVelocitySource - constantVelocityTarget;

		if (gamma < omega)  // Underdamped case
		{
			float omegaD = Mathf.Sqrt(omega * omega - gamma * gamma);

			// Calculate A and B using initial conditions
			Vector3 A = relativeInitialPosition;
			Vector3 B = (relativeInitialVelocity + gamma * A) / omegaD;

			// Calculate the exponential decay term
			float expTerm = Mathf.Exp(-gamma * time);

			// Calculate the position and velocity of the target at time t
			Vector3 positionTargetAtT = initialPositionTarget + constantVelocityTarget * time;

			// Calculate the relative position and velocity at time t
			Vector3 relativePositionAtT = expTerm * (A * Mathf.Cos(omegaD * time) + B * Mathf.Sin(omegaD * time));
			Vector3 relativeVelocityAtT = expTerm * (-A * omegaD * Mathf.Sin(omegaD * time) + B * omegaD * Mathf.Cos(omegaD * time))
										  - expTerm * gamma * (A * Mathf.Cos(omegaD * time) + B * Mathf.Sin(omegaD * time));

			// Calculate the absolute position and velocity of the source
			positionSource = relativePositionAtT + positionTargetAtT;
			velocitySource = relativeVelocityAtT + constantVelocityTarget;
		}
		else if (gamma == omega)  // Critically damped case
		{
			// Calculate coefficients based on initial conditions
			Vector3 C = relativeInitialPosition;
			Vector3 D = relativeInitialVelocity + gamma * C;

			// Calculate the exponential decay term
			float expTerm = Mathf.Exp(-gamma * time);

			// Calculate the position and velocity of the target at time t
			Vector3 positionTargetAtT = initialPositionTarget + constantVelocityTarget * time;

			// Calculate the relative position and velocity at time t
			Vector3 relativePositionAtT = expTerm * (C + D * time);
			Vector3 relativeVelocityAtT = expTerm * (D - gamma * (C + D * time));

			// Calculate the absolute position and velocity of the source
			positionSource = relativePositionAtT + positionTargetAtT;
			velocitySource = relativeVelocityAtT + constantVelocityTarget;
		}
		else  // Overdamped case (gamma > omega)
		{
			float alpha1 = -gamma + Mathf.Sqrt(gamma * gamma - omega * omega);
			float alpha2 = -gamma - Mathf.Sqrt(gamma * gamma - omega * omega);

			// Calculate coefficients based on initial conditions
			Vector3 C1 = (relativeInitialVelocity - alpha2 * relativeInitialPosition) / (alpha1 - alpha2);
			Vector3 C2 = (alpha1 * relativeInitialPosition - relativeInitialVelocity) / (alpha1 - alpha2);

			// Calculate the exponential terms
			float expAlpha1 = Mathf.Exp(alpha1 * time);
			float expAlpha2 = Mathf.Exp(alpha2 * time);

			// Calculate the position and velocity of the target at time t
			Vector3 positionTargetAtT = initialPositionTarget + constantVelocityTarget * time;

			// Calculate the relative position and velocity at time t
			Vector3 relativePositionAtT = C1 * expAlpha1 + C2 * expAlpha2;
			Vector3 relativeVelocityAtT = C1 * alpha1 * expAlpha1 + C2 * alpha2 * expAlpha2;

			// Calculate the absolute position and velocity of the source
			positionSource = relativePositionAtT + positionTargetAtT;
			velocitySource = relativeVelocityAtT + constantVelocityTarget;
		}
    }
}

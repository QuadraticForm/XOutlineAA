using UnityEngine;

public class TestFollower : MonoBehaviour
{
	public GameObject source;
	public GameObject target;

	GameObject Source
	{

	get { return source == null? gameObject : source; } 

	}
	

	public enum Timing
	{
		update,
		late_update,
		animator_move,
		animator_ik
	}

	public Timing timing = Timing.update;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

	void Follow()
	{
		Source.transform.position = target.transform.position;
	}

	
	// Update is called once per frame
	void Update()
    {
		if (target == null) return;

		if (timing != Timing.update) return;

		Follow();
    }

	private void LateUpdate()
	{
		if (target == null) return;

		if (timing != Timing.late_update) return;

		Follow();
	}

	private void OnAnimatorMove()
	{
		if (target == null) return;

		if (timing != Timing.animator_move) return;

		Follow();
	}

	private void OnAnimatorIK(int layerIndex)
	{
		if (target == null) return;

		if (timing != Timing.animator_ik) return;

		Follow();
	}
}

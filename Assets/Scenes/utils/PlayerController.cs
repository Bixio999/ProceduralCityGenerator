using UnityEngine;

[RequireComponent(typeof(Rigidbody))]

public class PlayerController : MonoBehaviour {

	[Range(0.0f, 30.0f)] public float movementSpeed = 10f;
	[Range(0.0f, 360.0f)] public float rotationSensitivity = 90f;

	void Start () {
		;
	}
	
	void FixedUpdate () {
		if (this.transform.position.y < 0)
            this.transform.position = new Vector3(this.transform.position.x, 0, this.transform.position.z);

		Rigidbody rb = GetComponent<Rigidbody> ();
		// gas and brake are converted into a translation forward/backward
		rb.MovePosition (transform.position
						 + transform.forward * movementSpeed * (Input.GetAxis ("Vertical") * Time.deltaTime));
		// steering is translated into a rotation
		rb.MoveRotation(Quaternion.Euler(0.0f, rotationSensitivity * (Input.GetAxis ("Horizontal") * Time.deltaTime), 0.0f)
			            * transform.rotation);
	}
}

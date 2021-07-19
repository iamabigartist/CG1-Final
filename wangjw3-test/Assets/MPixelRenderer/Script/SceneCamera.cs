using UnityEngine;

public class SceneCamera : MonoBehaviour
{
	public float m_zoomSpeed = 1.0f;
	public float m_dragSpeed = 10.0f;
	public float m_rotateSpeed = 5.0f;

	private bool m_isDraging;
	private Vector3 m_lastMousePos;
	private Transform m_trans;

	// Start is called before the first frame update
	private void Start ()
	{
		this.m_trans = transform;
	}

	private void LateUpdate ()
	{
		Zoom();
		Rotate();
		Drag();
	}

	private void Zoom ()
	{
		Vector3 pos = this.m_trans.position;
		if (Input.GetAxis( "Mouse ScrollWheel" ) < 0)
			pos -= this.m_trans.forward * this.m_zoomSpeed;

		if (Input.GetAxis( "Mouse ScrollWheel" ) > 0)
			pos += this.m_trans.forward * this.m_zoomSpeed;

		this.m_trans.position = pos;
	}

	private void Rotate ()
	{
		Vector3 angle = this.m_trans.eulerAngles;
		if (Input.GetMouseButton( 1 ))
		{
			float x = Input.GetAxis( "Mouse X" );
			float y = Input.GetAxis( "Mouse Y" );
			angle.x -= y * this.m_rotateSpeed;
			angle.y += x * this.m_rotateSpeed;
		}

		Quaternion rotation = Quaternion.Euler( angle );
		this.m_trans.rotation = rotation;
	}

	private int Log10 ( bool a ) { return a ? 1 : 2; }

	private void Drag ()
	{
		if (Input.GetMouseButton( 2 ))
		{
			if (this.m_isDraging == false)
			{
				this.m_isDraging = true;
				this.m_lastMousePos = Input.mousePosition;
			}
			else
			{
				Vector3 newMousePos = Input.mousePosition;
				Vector3 delta = newMousePos - this.m_lastMousePos;
				this.m_trans.position +=
					CalcDragLength( gameObject.GetComponent<Camera>(), delta ) * this.m_dragSpeed;
				this.m_lastMousePos = newMousePos;
			}
		}

		if (Input.GetMouseButtonUp( 2 )) this.m_isDraging = false;
	}

	private Vector3 CalcDragLength ( Camera camera, Vector2 mouseDelta )
	{
		float rectHeight = -1;
		float rectWidth = -1;
		if (camera.orthographic)
			rectHeight = 2 * camera.orthographicSize;
		//rectWidth = rectHeight / camera.aspect;
		else rectHeight = 2 * Mathf.Tan( camera.fieldOfView * 0.5f * Mathf.Deg2Rad );
		rectWidth = Screen.width * rectHeight / Screen.height;
		Vector3 moveDir = -rectWidth / Screen.width  * mouseDelta.x * camera.transform.right -
						  rectHeight / Screen.height * mouseDelta.y * camera.transform.up;

		return moveDir;
	}
}
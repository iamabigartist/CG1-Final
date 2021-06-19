using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class SceneCamera : MonoBehaviour
{
    private Transform m_trans;
    public float m_zoomSpeed = 1.0f;
    public float m_dragSpeed = 10.0f;
    public float m_rotateSpeed = 5.0f;
    private Vector3 m_lastMousePos;

    private bool m_isDraging = false;

    // Start is called before the first frame update
    private void Start ()
    {
        m_trans = this.transform;
    }

    private void LateUpdate ()
    {
        Zoom();
        Rotate();
        Drag();
    }

    private void Zoom ()
    {
        Vector3 pos = m_trans.position;
        if ( Input.GetAxis( "Mouse ScrollWheel" ) < 0 )
        {
            pos -= m_trans.forward * m_zoomSpeed;
        }

        if ( Input.GetAxis( "Mouse ScrollWheel" ) > 0 )
        {
            pos += m_trans.forward * m_zoomSpeed;
        }

        m_trans.position = pos;
    }

    private void Rotate ()
    {
        Vector3 angle = m_trans.eulerAngles;
        if ( Input.GetMouseButton( 1 ) )
        {
            float x = Input.GetAxis( "Mouse X" );
            float y = Input.GetAxis( "Mouse Y" );
            angle.x -= y * m_rotateSpeed;
            angle.y += x * m_rotateSpeed;
        }

        Quaternion rotation = Quaternion.Euler( angle );
        m_trans.rotation = rotation;
    }

    private void Drag ()
    {
        if ( Input.GetMouseButton( 2 ) )
        {
            if ( m_isDraging == false )
            {
                m_isDraging = true;
                m_lastMousePos = Input.mousePosition;
            }
            else
            {
                Vector3 newMousePos = Input.mousePosition;
                Vector3 delta = newMousePos - m_lastMousePos;
                m_trans.position += CalcDragLength( gameObject.GetComponent<Camera>() , delta ) * m_dragSpeed;
                m_lastMousePos = newMousePos;
            }
        }

        if ( Input.GetMouseButtonUp( 2 ) )
        {
            m_isDraging = false;
        }
    }

    private Vector3 CalcDragLength ( Camera camera , Vector2 mouseDelta )
    {
        float rectHeight = -1;
        float rectWidth = -1;
        if ( camera.orthographic )
        {
            rectHeight = 2 * camera.orthographicSize;
            //rectWidth = rectHeight / camera.aspect;
        }
        else
        {
            rectHeight = 2 * Mathf.Tan( camera.fieldOfView * 0.5f * Mathf.Deg2Rad );
        }
        rectWidth = Screen.width * rectHeight / Screen.height;
        Vector3 moveDir = -rectWidth / Screen.width * mouseDelta.x * camera.transform.right - rectHeight / Screen.height * mouseDelta.y * camera.transform.up;

        return moveDir;
    }
}
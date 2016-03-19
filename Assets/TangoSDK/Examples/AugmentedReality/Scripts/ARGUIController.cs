//-----------------------------------------------------------------------
// <copyright file="ARGUIController.cs" company="Google">
//
// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using Tango;
using UnityEngine;

/// <summary>
/// GUI controller controls all the debug overlay to show the data for poses.
/// </summary>
public class ARGUIController : MonoBehaviour, ITangoLifecycle, ITangoDepth
{
    public const float UI_BUTTON_SIZE_X = 250.0f;
    public const float UI_BUTTON_SIZE_Y = 130.0f;
    public const float UI_BUTTON_GAP_X = 5.0f;

    /// <summary>
    /// The marker prefab to place on taps.
    /// </summary>
    public GameObject m_prefabMarker;

    /// <summary>
    /// The touch effect to place on taps.
    /// </summary>
    public RectTransform m_prefabTouchEffect;

    /// <summary>
    /// The canvas to place 2D game objects under.
    /// </summary>
    public Canvas m_canvas;

    /// <summary>
    /// The point cloud object in the scene.
    /// </summary>
    public TangoPointCloud m_pointCloud;

    private TangoApplication m_tangoApplication;

    /// <summary>
    /// If set, then the depth camera is on and we are waiting for the next depth update.
    /// </summary>
    private bool m_findPlaneWaitingForDepth;

    /// <summary>
    /// If set, this is the selected marker.
    /// </summary>
    private ARMarker m_selectedMarker;

    /// <summary>
    /// If set, this is the rectangle bounding the selected marker.
    /// </summary>
    private Rect m_selectedRect;

    /// <summary>
    /// If set, this is the rectangle for the Hide All button.
    /// </summary>
    private Rect m_hideAllRect;

    /// <summary>
    /// Unity Start() callback, we set up some initial values here.
    /// </summary>
    public void Start()
    {
        m_tangoApplication = FindObjectOfType<TangoApplication>();
        m_tangoApplication.Register(this);
    }

    /// <summary>
    /// Updates UI and handles player input.
    /// </summary>
    public void Update()
    {
        _UpdateLocationMarker();

        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    /// <summary>
    /// Display simple GUI.
    /// </summary>
    public void OnGUI()
    {
	if (GUI.Button (new Rect (10,70, 100, 20), "This is text"))
	{
		
	}

        if (m_selectedMarker != null)
        {
            Renderer selectedRenderer = m_selectedMarker.GetComponent<Renderer>();

            // GUI's Y is flipped from the mouse's Y
            Rect screenRect = WorldBoundsToScreen(Camera.main, selectedRenderer.bounds);
            float yMin = Screen.height - screenRect.yMin;
            float yMax = Screen.height - screenRect.yMax;
            screenRect.yMin = Mathf.Min(yMin, yMax);
            screenRect.yMax = Mathf.Max(yMin, yMax);

            if (GUI.Button(screenRect, "<size=30>Hide</size>"))
            {
                m_selectedMarker.SendMessage("Hide");
                m_selectedMarker = null;
                m_selectedRect = new Rect();
            }
            else
            {
                m_selectedRect = screenRect;
            }
        }
        else
        {
            m_selectedRect = new Rect();
        }

        if (GameObject.FindObjectOfType<ARMarker>() != null)
        {
            m_hideAllRect = new Rect(Screen.width - UI_BUTTON_SIZE_X - UI_BUTTON_GAP_X,
                                     Screen.height - UI_BUTTON_SIZE_Y - UI_BUTTON_GAP_X,
                                     UI_BUTTON_SIZE_X,
                                     UI_BUTTON_SIZE_Y);
            if (GUI.Button(m_hideAllRect, "<size=30>Hide All</size>"))
            {
                foreach (ARMarker marker in GameObject.FindObjectsOfType<ARMarker>())
                {
                    marker.SendMessage("Hide");
                }
            }
        }
        else
        {
            m_hideAllRect = new Rect(0, 0, 0, 0);
        }
    }
    
    /// <summary>
    /// This is called when the permission granting process is finished.
    /// </summary>
    /// <param name="permissionsGranted"><c>true</c> if permissions were granted, otherwise <c>false</c>.</param>
    public void OnTangoPermissions(bool permissionsGranted)
    {
    }
    
    /// <summary>
    /// This is called when succesfully connected to the Tango service.
    /// </summary>
    public void OnTangoServiceConnected()
    {
        m_tangoApplication.SetDepthCameraRate(TangoEnums.TangoDepthCameraRate.DISABLED);
    }
    
    /// <summary>
    /// This is called when disconnected from the Tango service.
    /// </summary>
    public void OnTangoServiceDisconnected()
    {
    }

    /// <summary>
    /// This is called each time new depth data is available.
    /// 
    /// On the Tango tablet, the depth callback occurs at 5 Hz.
    /// </summary>
    /// <param name="tangoDepth">Tango depth.</param>
    public void OnTangoDepthAvailable(TangoUnityDepth tangoDepth)
    {
        // Don't handle depth here because the PointCloud may not have been updated yet.  Just
        // tell the coroutine it can continue.
        m_findPlaneWaitingForDepth = false;
    }

    /// <summary>
    /// Convert a 3D bounding box into a 2D Rect.
    /// </summary>
    /// <returns>The 2D Rect in Screen coordinates.</returns>
    /// <param name="cam">Camera to use.</param>
    /// <param name="bounds">3D bounding box.</param>
    private Rect WorldBoundsToScreen(Camera cam, Bounds bounds)
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;
        Bounds screenBounds = new Bounds(cam.WorldToScreenPoint(center), Vector3.zero);

        screenBounds.Encapsulate(cam.WorldToScreenPoint(center + new Vector3(+extents.x, +extents.y, +extents.z)));
        screenBounds.Encapsulate(cam.WorldToScreenPoint(center + new Vector3(+extents.x, +extents.y, -extents.z)));
        screenBounds.Encapsulate(cam.WorldToScreenPoint(center + new Vector3(+extents.x, -extents.y, +extents.z)));
        screenBounds.Encapsulate(cam.WorldToScreenPoint(center + new Vector3(+extents.x, -extents.y, -extents.z)));
        screenBounds.Encapsulate(cam.WorldToScreenPoint(center + new Vector3(-extents.x, +extents.y, +extents.z)));
        screenBounds.Encapsulate(cam.WorldToScreenPoint(center + new Vector3(-extents.x, +extents.y, -extents.z)));
        screenBounds.Encapsulate(cam.WorldToScreenPoint(center + new Vector3(-extents.x, -extents.y, +extents.z)));
        screenBounds.Encapsulate(cam.WorldToScreenPoint(center + new Vector3(-extents.x, -extents.y, -extents.z)));
        return Rect.MinMaxRect(screenBounds.min.x, screenBounds.min.y, screenBounds.max.x, screenBounds.max.y);
    }

    /// <summary>
    /// Update location marker state.
    /// </summary>
    private void _UpdateLocationMarker()
    {
        if (Input.touchCount == 1)
        {
            // Single tap -- place new location or select existing location.
            Touch t = Input.GetTouch(0);
            Vector2 guiPosition = new Vector2(t.position.x, Screen.height - t.position.y);
            Camera cam = Camera.main;
            RaycastHit hitInfo;

            if (t.phase != TouchPhase.Began)
            {
                return;
            }

            if (m_selectedRect.Contains(guiPosition) || m_hideAllRect.Contains(guiPosition))
            {
                // do nothing, the button will handle it
            }
            else if (Physics.Raycast(cam.ScreenPointToRay(t.position), out hitInfo))
            {
                // Found a marker, select it (so long as it isn't disappearing)!
                GameObject tapped = hitInfo.collider.gameObject;
                if (!tapped.GetComponent<Animation>().isPlaying)
                {
                    m_selectedMarker = tapped.GetComponent<ARMarker>();
                }
            }
            else
            {
                // Place a new point at that location, clear selection
                m_selectedMarker = null;
                StartCoroutine(_WaitForDepthAndFindPlane(t.position));

                // Because we may wait a small amount of time, this is a good place to play a small
                // animation so the user knows that their input was received.
                RectTransform touchEffectRectTransform = (RectTransform)Instantiate(m_prefabTouchEffect);
                touchEffectRectTransform.transform.SetParent(m_canvas.transform, false);
                Vector2 normalizedPosition = t.position;
                normalizedPosition.x /= Screen.width;
                normalizedPosition.y /= Screen.height;
                touchEffectRectTransform.anchorMin = touchEffectRectTransform.anchorMax = normalizedPosition;
            }
        }
    }

    /// <summary>
    /// Wait for the next depth update, then find the plane at the touch position.
    /// </summary>
    /// <returns>Coroutine IEnumerator.</returns>
    /// <param name="touchPosition">Touch position to find a plane at.</param>
    private IEnumerator _WaitForDepthAndFindPlane(Vector2 touchPosition)
    {
        m_findPlaneWaitingForDepth = true;

        // Turn on the camera and wait for a single depth update.
        m_tangoApplication.SetDepthCameraRate(TangoEnums.TangoDepthCameraRate.MAXIMUM);
        while (m_findPlaneWaitingForDepth)
        {
            yield return null;
        }

        m_tangoApplication.SetDepthCameraRate(TangoEnums.TangoDepthCameraRate.DISABLED);

        // Find the plane.
        Camera cam = Camera.main;
        Vector3 planeCenter;
        Plane plane;
        if (!m_pointCloud.FindPlane(cam, touchPosition, out planeCenter, out plane))
        {
            yield break;
        }

        // Ensure the location is always facing the camera.  This is like a LookRotation, but for the Y axis.
        Vector3 up = plane.normal;
        Vector3 forward;
        if (Vector3.Angle(plane.normal, cam.transform.forward) < 175)
        {
            Vector3 right = Vector3.Cross(up, cam.transform.forward).normalized;
            forward = Vector3.Cross(right, up).normalized;
        }
        else
        {
            // Normal is nearly parallel to camera look direction, the cross product would have too much
            // floating point error in it.
            forward = Vector3.Cross(up, cam.transform.right);
        }

        Instantiate(m_prefabMarker, planeCenter, Quaternion.LookRotation(forward, up));
        m_selectedMarker = null;
    }
}

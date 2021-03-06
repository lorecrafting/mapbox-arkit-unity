﻿namespace Mapbox.Unity.Ar
{
	using Mapbox.Unity.Map;
	using Mapbox.Unity.Location;
	using UnityEngine;
	using Mapbox.Unity.Utilities;
	using UnityEngine.XR.iOS;
	using System;

	public class ManualSynchronizationContextBehaviour : MonoBehaviour, ISynchronizationContext
	{
		[SerializeField]
		MapAtCurrentLocation _map;

		[SerializeField]
		Transform _mapCamera;

		[SerializeField]
		TransformLocationProvider _locationProvider;

		[SerializeField]
		AbstractAlignmentStrategy _alignmentStrategy;

		float _lastHeight;
		float _lastHeading;

		public event Action<Alignment> OnAlignmentAvailable = delegate { };

		void Start()
		{
			_alignmentStrategy.Register(this);
			_map.OnInitialized += Map_OnInitialized;
			UnityARSessionNativeInterface.ARAnchorAddedEvent += AnchorAdded;
		}

		void OnDestroy()
		{
			_alignmentStrategy.Unregister(this);
		}

		void Map_OnInitialized()
		{
			_map.OnInitialized -= Map_OnInitialized;
			_locationProvider.OnHeadingUpdated += LocationProvider_OnHeadingUpdated;
			_locationProvider.OnLocationUpdated += LocationProvider_OnLocationUpdated;
		}

		void LocationProvider_OnHeadingUpdated(object sender, Unity.Location.HeadingUpdatedEventArgs e)
		{
			_lastHeading = e.Heading;
		}

		void LocationProvider_OnLocationUpdated(object snder, LocationUpdatedEventArgs e)
		{
			var alignment = new Alignment();
			var originalPosition = _map.Root.position;
			alignment.Rotation = -_lastHeading + _map.Root.localEulerAngles.y;

			// Rotate our offset by the last heading.
			var rotation = Quaternion.Euler(0, -_lastHeading, 0);
			alignment.Position = rotation * (-Conversions.GeoToWorldPosition(e.Location, _map.CenterMercator, _map.WorldRelativeScale).ToVector3xz() + originalPosition);
			alignment.Position.y = _lastHeight;

			OnAlignmentAvailable(alignment);

			// Reset camera to avoid confusion.
			var mapCameraPosition = Vector3.zero;
			mapCameraPosition.y = _mapCamera.localPosition.y;
			var mapCameraRotation = Vector3.zero;
			mapCameraRotation.x = _mapCamera.localEulerAngles.x;
			_mapCamera.localPosition = mapCameraPosition;
			_mapCamera.eulerAngles = mapCameraRotation;
		}

		void AnchorAdded(ARPlaneAnchor anchorData)
		{
			_lastHeight = UnityARMatrixOps.GetPosition(anchorData.transform).y;
			Unity.Utilities.Console.Instance.Log(string.Format("AR Plane Height: {0}", _lastHeight), "yellow");
		}
	}
}
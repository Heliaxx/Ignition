using Godot;
using System.Collections.Generic;

public partial class Kaito
{
	// Targeting
	private GimbalTarget _lockedTarget;
	private const float TARGET_MAX_RANGE = 2000f;
	public GimbalTarget LockedTarget => _lockedTarget;

	public const float MissileLockTime = 2f;
	private float _missileLockTimer = 0f;
	public float MissileLockProgress => _lockedTarget != null ? Mathf.Clamp(_missileLockTimer / MissileLockTime, 0f, 1f) : 0f;
	public bool IsMissileLocked => _lockedTarget != null && _missileLockTimer >= MissileLockTime;

	// Target HUD elements
	private Control _targetPanel;
	private ProgressBar _targetHealthBar;
	private Label3D _targetNameDisplay3D;
	private Label3D _targetDistDisplay3D;

	private void InitTargeting()
	{
		_targetPanel = canvasLayer.GetNodeOrNull<Control>("TargetPanel");
		if (_targetPanel != null)
		{
			_targetHealthBar = _targetPanel.GetNode<ProgressBar>("MarginContainer/VBoxContainer/TargetHealthBar");
			_targetPanel.Visible = false;
		}
		_targetNameDisplay3D = GetNodeOrNull<Label3D>("TargetNameDisplay");
		_targetDistDisplay3D = GetNodeOrNull<Label3D>("TargetDistDisplay");
	}

	private void CycleTarget()
	{
		var camera = GetViewport().GetCamera3D();
		if (camera == null) { ClearTarget(); return; }

		Vector2 screenCenter = GetViewport().GetVisibleRect().Size / 2f;

		var gimbalTargets = GetTree().GetNodesInGroup("gimbal_targets");
		var validTargets = new List<(GimbalTarget target, float screenDist)>();

		foreach (var node in gimbalTargets)
		{
			if (node is not GimbalTarget gt || !gt.IsValid())
				continue;

			float worldDist = GlobalPosition.DistanceTo(gt.GlobalPosition);
			if (worldDist > TARGET_MAX_RANGE) continue;
			if (camera.IsPositionBehind(gt.GlobalPosition)) continue;

			Vector2 screenPos = camera.UnprojectPosition(gt.GlobalPosition);
			float screenDist = screenPos.DistanceTo(screenCenter);
			validTargets.Add((gt, screenDist));
		}

		if (validTargets.Count == 0)
		{
			ClearTarget();
			return;
		}

		// Sort by distance from screen center
		validTargets.Sort((a, b) => a.screenDist.CompareTo(b.screenDist));

		if (_lockedTarget == null || !IsInstanceValid(_lockedTarget))
		{
			SetTarget(validTargets[0].target);
		}
		else
		{
			// Has a target - find a different one closer to center, or untarget
			GimbalTarget best = null;
			foreach (var (gt, _) in validTargets)
			{
				if (gt != _lockedTarget)
				{
					best = gt;
					break;
				}
			}

			if (best != null)
				SetTarget(best);
			else
				ClearTarget();
		}
	}

	private void SetTarget(GimbalTarget target)
	{
		if (_lockedTarget == target) return;

		if (_lockedTarget != null && IsInstanceValid(_lockedTarget))
			_lockedTarget.Died -= OnTargetDied;

		_lockedTarget = target;
		_missileLockTimer = 0f;

		if (_lockedTarget != null)
			_lockedTarget.Died += OnTargetDied;
	}

	private void ClearTarget()
	{
		if (_lockedTarget != null && IsInstanceValid(_lockedTarget))
			_lockedTarget.Died -= OnTargetDied;

		_lockedTarget = null;
		_missileLockTimer = 0f;
	}

	private void OnTargetDied()
	{
		_lockedTarget = null;
		_missileLockTimer = 0f;
	}

	private void UpdateTargetHUD(float delta)
	{
		if (_targetPanel == null) return;

		// Validate target is still alive and in range
		if (_lockedTarget != null)
		{
			if (!IsInstanceValid(_lockedTarget) || !_lockedTarget.IsValid())
			{
				ClearTarget();
			}
			else
			{
				float dist = GlobalPosition.DistanceTo(_lockedTarget.GlobalPosition);
				if (dist > TARGET_MAX_RANGE)
					ClearTarget();
			}
		}

		if (_lockedTarget == null)
		{
			if (_targetPanel != null) _targetPanel.Visible = false;
			if (_targetNameDisplay3D != null) _targetNameDisplay3D.Text = "---";
			if (_targetDistDisplay3D != null) _targetDistDisplay3D.Text = "---";
			return;
		}

		if (_missileLockTimer < MissileLockTime)
		{
			if (IsTargetInGimbalCone())
				_missileLockTimer += delta;
			else
				_missileLockTimer = 0f;
		}

		if (_targetPanel != null) _targetPanel.Visible = true;
		float distance = GlobalPosition.DistanceTo(_lockedTarget.GlobalPosition);
		if (_targetNameDisplay3D != null) _targetNameDisplay3D.Text = _lockedTarget.GetDisplayName();
		if (_targetDistDisplay3D != null) _targetDistDisplay3D.Text = $"{distance:F0}m";

		_targetHealthBar.Visible = _lockedTarget.HasHealthData();
		if (_lockedTarget.HasHealthData())
		{
			_targetHealthBar.MaxValue = _lockedTarget.GetMaxHealth();
			_targetHealthBar.Value    = _lockedTarget.GetCurrentHealth();
		}
	}

	private bool IsTargetInGimbalCone()
	{
		if (_lockedTarget == null || barrel == null) return false;
		Vector3 toTarget = (_lockedTarget.GlobalPosition - barrel.GlobalPosition).Normalized();
		Vector3 forward = (-barrel.GlobalTransform.Basis.Z).Normalized();
		return forward.AngleTo(toTarget) <= Mathf.DegToRad(GimbalAngle);
	}

	public Vector2? GetLockedTargetScreenPos()
	{
		if (_lockedTarget == null || !IsInstanceValid(_lockedTarget))
			return null;

		var camera = GetViewport().GetCamera3D();
		if (camera == null) return null;

		if (camera.IsPositionBehind(_lockedTarget.GlobalPosition))
			return null;

		return camera.UnprojectPosition(_lockedTarget.GlobalPosition);
	}

	public Vector2? GetLeadTargetScreenPos()
	{
		if (_lockedTarget == null || !IsInstanceValid(_lockedTarget))
			return null;

		var camera = GetViewport().GetCamera3D();
		if (camera == null) return null;

		Vector3 targetPos = _lockedTarget.GlobalPosition;
		Vector3 targetVel = _lockedTarget.GetVelocity();
		Vector3 shooterPos = GlobalPosition;

		float bulletWorldSpeed = BulletSpeed + Mathf.Max(0, Velocity.Dot((-GlobalTransform.Basis.Z).Normalized()));

		float dist = shooterPos.DistanceTo(targetPos);
		Vector3 leadPos = targetPos;
		for (int i = 0; i < 2; i++)
		{
			float tof = dist / bulletWorldSpeed;
			leadPos = targetPos + targetVel * tof;
			dist = shooterPos.DistanceTo(leadPos);
		}

		if (camera.IsPositionBehind(leadPos))
			return null;

		return camera.UnprojectPosition(leadPos);
	}
}

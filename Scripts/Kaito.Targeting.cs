using Godot;
using System.Collections.Generic;

public partial class Kaito
{
	// Targeting
	private GimbalTarget _lockedTarget;
	private const float TARGET_MAX_RANGE = 2000f;
	public GimbalTarget LockedTarget => _lockedTarget;

	// Target HUD elements
	private Control _targetPanel;
	private Label _targetNameLabel;
	private Label _targetDistanceLabel;
	private ProgressBar _targetHealthBar;

	private void InitTargeting()
	{
		_targetPanel = canvasLayer.GetNodeOrNull<Control>("TargetPanel");
		if (_targetPanel != null)
		{
			_targetNameLabel = _targetPanel.GetNode<Label>("MarginContainer/VBoxContainer/TargetName");
			_targetDistanceLabel = _targetPanel.GetNode<Label>("MarginContainer/VBoxContainer/DistanceLabel");
			_targetHealthBar = _targetPanel.GetNode<ProgressBar>("MarginContainer/VBoxContainer/TargetHealthBar");
			_targetPanel.Visible = false;
		}
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

		if (_lockedTarget != null)
			_lockedTarget.Died += OnTargetDied;
	}

	private void ClearTarget()
	{
		if (_lockedTarget != null && IsInstanceValid(_lockedTarget))
			_lockedTarget.Died -= OnTargetDied;

		_lockedTarget = null;
	}

	private void OnTargetDied()
	{
		_lockedTarget = null;
	}

	private void UpdateTargetHUD()
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
			_targetPanel.Visible = false;
			return;
		}

		_targetPanel.Visible = true;
		float distance = GlobalPosition.DistanceTo(_lockedTarget.GlobalPosition);
		_targetNameLabel.Text = _lockedTarget.GetDisplayName();
		_targetDistanceLabel.Text = $"{distance:F0}m";

		_targetHealthBar.Visible = _lockedTarget.HasHealthData();
		if (_lockedTarget.HasHealthData())
		{
			_targetHealthBar.MaxValue = _lockedTarget.GetMaxHealth();
			_targetHealthBar.Value    = _lockedTarget.GetCurrentHealth();
		}
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

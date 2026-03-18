using Godot;
using System.Collections.Generic;

public partial class Kaito
{
	// Targeting
	private Fighter _lockedTarget;
	private const float TARGET_MAX_RANGE = 2000f;
	public Fighter LockedTarget => _lockedTarget;

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

		var enemies = GetTree().GetNodesInGroup("enemies");
		var validTargets = new List<(Fighter fighter, float screenDist)>();

		foreach (var node in enemies)
		{
			if (node is Fighter fighter && fighter.Visible && fighter.ProcessMode != ProcessModeEnum.Disabled
				&& fighter.CurrentHealthValue > 0)
			{
				float worldDist = GlobalPosition.DistanceTo(fighter.GlobalPosition);
				if (worldDist > TARGET_MAX_RANGE) continue;
				if (camera.IsPositionBehind(fighter.GlobalPosition)) continue;

				Vector2 screenPos = camera.UnprojectPosition(fighter.GlobalPosition);
				float screenDist = screenPos.DistanceTo(screenCenter);
				validTargets.Add((fighter, screenDist));
			}
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
			// No current target — pick closest to center
			SetTarget(validTargets[0].fighter);
		}
		else
		{
			// Has a target — find a different one closer to center, or untarget
			Fighter best = null;
			foreach (var (fighter, screenDist) in validTargets)
			{
				if (fighter != _lockedTarget)
				{
					best = fighter;
					break; // first non-current is closest to center
				}
			}

			if (best != null)
				SetTarget(best);
			else
				ClearTarget();
		}
	}

	private void SetTarget(Fighter target)
	{
		if (_lockedTarget == target) return;

		// Disconnect old signal
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
			if (!IsInstanceValid(_lockedTarget) || !_lockedTarget.Visible
				|| _lockedTarget.ProcessMode == ProcessModeEnum.Disabled
				|| _lockedTarget.CurrentHealthValue <= 0)
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
		_targetNameLabel.Text = _lockedTarget.DisplayName;
		_targetDistanceLabel.Text = $"{distance:F0}m";
		_targetHealthBar.MaxValue = _lockedTarget.MaxHealthValue;
		_targetHealthBar.Value = _lockedTarget.CurrentHealthValue;
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
		Vector3 targetVel = _lockedTarget.Velocity;
		Vector3 shooterPos = GlobalPosition;

		// Effective bullet speed in world space (bullet local speed + ship forward speed)
		float bulletWorldSpeed = BulletSpeed + Mathf.Max(0, Velocity.Dot((-GlobalTransform.Basis.Z).Normalized()));

		// Iterative lead prediction (2 iterations for accuracy)
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

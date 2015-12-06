﻿using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    public float movementSpeed = 5f;
    public float maxAngle = 30f;
    public float maxHandsAngle = 10f;

    private Vector2 velocity;
    private GameMain gameMain;
	
    public Dictionary <string, PlayerLimb> limbs;
	public string[] limbsNames = {"left_hand", "right_hand", "left_leg", "right_leg"};

    private int missingLimbsCount = 0;

    public AudioClip[] Sounds;

    public bool GodMode = false;
    private float blinkingDelay = 100;
    private float blinkingTimer = 0;
    private bool visible = true;

    public float SpeedUpDuration = 5;
    private List<PlayerLimb> detachedLimbs = new List<PlayerLimb>();
    public float detachedLimbsRotationSpeed = 500f;

	void Start () 
    {
        gameMain = Camera.main.GetComponent<GameMain>();
                        
        limbs = new Dictionary <string, PlayerLimb>();
		foreach (var name in limbsNames)
		{
			limbs[name] = transform.Find(name).GetComponent<PlayerLimb>();
		}
		RestoreAllLimbs();
    }

	void Update ()
    {
        if (gameMain.IsPaused || gameMain.IsDead)
        {
            return;
        }

        if (GodMode)
        {
            if (blinkingTimer >= blinkingDelay)
            {
                blinkingTimer = 0;
                ToggleVisibility();
            } 
            else
            {
                blinkingTimer += Time.deltaTime * 1000;
            }
        } 
        else
        {
            if (!visible)
                ToggleVisibility();
        }

        // Замедление при потере конечностей
        float missingLimbsMul = 1f - missingLimbsCount / 4f;
        
        // Скорость
        velocity = Vector2.Lerp(velocity, JoystickInput.input * missingLimbsMul, 10f * Time.deltaTime);

        // Обновление позиции
        float cameraAngle = Camera.main.transform.eulerAngles.y;
        Vector2 movement = Utils.RotateVector2(velocity * movementSpeed * Time.deltaTime, -cameraAngle);
        transform.Translate(new Vector3(movement.x, 0f, movement.y), Space.World);
        
        // Столкновения с боковыми стенами
        float maxPosition = gameMain.pipeSize / 2f - transform.localScale.x / 2f;
        transform.position = new Vector3(
            Mathf.Clamp(transform.position.x, -maxPosition, maxPosition), 
            transform.position.y, 
            Mathf.Clamp(transform.position.z, -maxPosition, maxPosition));
        
        // Конечности
        float missingLegsRotationHandsAdd = 0f;
        float missingLegsRotationBodyAdd = 0f;
        // Оторванные ноги
        if (!limbs["left_leg"].State || !limbs["right_leg"].State)
        {
            missingLegsRotationHandsAdd = Mathf.Sin(Time.time * 16f) * 8f;
            missingLegsRotationBodyAdd = Mathf.Sin(Time.time * 3f) * 3f * missingLimbsCount;
        }
        // Поворот рук
        float handsAngle = -JoystickInput.input.x * maxHandsAngle;
        limbs["left_hand"].transform.localRotation = Quaternion.Euler(0f, 0f, handsAngle + missingLegsRotationHandsAdd);
        limbs["right_hand"].transform.localRotation = Quaternion.Euler(0f, 0f, handsAngle - missingLegsRotationHandsAdd);
    
        // Вращение туловища
        var bodyAngle = -maxAngle * velocity.x - cameraAngle + missingLegsRotationBodyAdd;
        transform.rotation = Quaternion.Euler(90f, 0f, bodyAngle);
        
        // Движение оторванных конечностей
        for (int i = 0; i < detachedLimbs.Count; i++)
        {
            var limb = detachedLimbs[i];
            limb.transform.Rotate(0f, 0f, detachedLimbsRotationSpeed * Time.deltaTime * limb.detachedRotationSpeedMul);
            limb.transform.Translate(Vector3.up * gameMain.FallingSpeed * Time.deltaTime * 2f + limb.detachedVelocity * Time.deltaTime, Space.World);
            if (limb.transform.position.y > 0)
            {
                Destroy(limb.gameObject);
                detachedLimbs.RemoveAt(i);
                i--;
            }
        }
    }

    public void OnPowerUpTaken(PowerUp.PowerUpType type)
    {
        Debug.Log(type);
        switch (type)
        {
            case PowerUp.PowerUpType.Ring:
                break;
            case PowerUp.PowerUpType.HealthKit:
                RestoreAllLimbs();
                break;
            case PowerUp.PowerUpType.SpeedUp:
                gameMain.ChangeFallingSpeed(20, 5);
                GodMode = true;
                break;
        }
    }

    public GameObject HitTestPlane(GameObject plane)
    {
        if (GodMode)
            return null;

        var planeBehaviour = plane.GetComponent<PlaneBehaviour>();
        var hitPlane = planeBehaviour.HitTestPoint(transform.position);
        if (hitPlane != null)
        {
            return hitPlane;
        }
        foreach (var name in limbsNames)
        {
            if (planeBehaviour.HitTestPoint(GetLimbPosition(name)) && limbs[name].State)
            {
                limbs[name].SetState(false); 
                CreateDetachedLimb(limbs[name]);

                var audioSource = gameObject.GetComponent<AudioSource>();
                audioSource.clip = Sounds[1];
                audioSource.Play();

                missingLimbsCount++;
            }
        }
        return null;
    }
    
    public void CreateDetachedLimb(PlayerLimb limb)
    {
        var limbClone = (PlayerLimb)Instantiate(limb, limb.transform.position, limb.transform.rotation);
        limbClone.detachedRotationSpeedMul = (Random.Range(80f, 150f) - 100f) / 4f;
        limbClone.detachedVelocity = new Vector3(limbClone.collisionOffset.x, 0f, limbClone.collisionOffset.y) * Random.Range(4f, 7f);
        detachedLimbs.Add(limbClone);
    }
    
    // Восстановить все части тела
	public void RestoreAllLimbs()
	{
		foreach (var name in limbsNames)
		{
			limbs[name].SetState(true);
		}
        missingLimbsCount = 0;
	}
	
	public Vector3 GetLimbPosition(string name)
	{
        var limbOffset = limbs[name].collisionOffset;   
		return transform.TransformPoint(limbOffset.x, limbOffset.y, 0f);
	}

    private void ToggleVisibility()
    {
        var childrenRenderers = gameObject.transform.GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in childrenRenderers)
        {
            visible = renderer.enabled = !renderer.enabled;
        }
    }
}
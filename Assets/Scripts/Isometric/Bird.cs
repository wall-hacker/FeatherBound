using UnityEngine;
using System.Collections;

public class Bird : MonoBehaviour {
	enum birdBehaviors{
		sing,
		preen,
		ruffle,
		peck,
		hopForward,
		hopBackward,
		hopLeft,
		hopRight,
	}

	public AudioClip song1;
	public AudioClip song2;
	public AudioClip flyAway1;
	public AudioClip flyAway2;

	public bool fleeCrows = true;

	Animator anim;

    [SerializeField]
	lb_BirdController controller;

	bool paused = false;
	bool idle = true;
	bool flying = false;
	bool landing = false;
	bool perched = false;
	bool onGround = true;
	bool dead = false;
	BoxCollider birdCollider;
	Vector3 bColCenter;
	Vector3 bColSize;
	SphereCollider solidCollider;
	float distanceToTarget = 0.0f;
	float agitationLevel = .5f;
	float originalAnimSpeed = 1.0f;
	Vector3 originalVelocity = Vector3.zero;

	//hash variables for the animation states and animation properties
	int idleAnimationHash;
	int singAnimationHash;
	int ruffleAnimationHash;
	int preenAnimationHash;
	int peckAnimationHash;
	int hopForwardAnimationHash;
	int hopBackwardAnimationHash;
	int hopLeftAnimationHash;
	int hopRightAnimationHash;
	int worriedAnimationHash;
	int landingAnimationHash;
	int flyAnimationHash;
	int hopIntHash;
	int flyingBoolHash;
	//int perchedBoolHash;
	int peckBoolHash;
	int ruffleBoolHash;
	int preenBoolHash;
	//int worriedBoolHash;
	int landingBoolHash;
	int singTriggerHash;
	int flyingDirectionHash;
	int dieTriggerHash;
    bool canHopLeft;
    bool canHopRight;
    bool canHopForward;
    bool canHopBackward;

	void Awake(){
		anim = gameObject.GetComponent<Animator>();
	}

	void OnEnable () {
		birdCollider = gameObject.GetComponent<BoxCollider>();
		bColCenter = birdCollider.center;
		bColSize = birdCollider.size;
		solidCollider = gameObject.GetComponent<SphereCollider>();
		anim = gameObject.GetComponent<Animator>();

		idleAnimationHash = Animator.StringToHash("Base Layer.Idle");
		flyAnimationHash = Animator.StringToHash ("Base Layer.fly");
		hopIntHash = Animator.StringToHash ("hop");
		flyingBoolHash = Animator.StringToHash("flying");
		peckBoolHash = Animator.StringToHash("peck");
		ruffleBoolHash = Animator.StringToHash("ruffle");
		preenBoolHash = Animator.StringToHash("preen");
		landingBoolHash = Animator.StringToHash("landing");
		singTriggerHash = Animator.StringToHash ("sing");
		flyingDirectionHash = Animator.StringToHash("flyingDirectionX");
		dieTriggerHash = Animator.StringToHash ("die");
		anim.SetFloat ("IdleAgitated",agitationLevel);

        canHopLeft = true;
        canHopRight = true;
        canHopForward = true;
        canHopBackward = true;
		if (dead){
			Revive();
		}
	}

	void PauseBird(){
		if(!dead){
			originalAnimSpeed = anim.speed;
			anim.speed = 0;
			if(!GetComponent<Rigidbody>().isKinematic){originalVelocity = GetComponent<Rigidbody>().linearVelocity;}
			GetComponent<Rigidbody>().isKinematic = true;
			GetComponent<AudioSource>().Stop ();
			paused = true;
		}
	}

	void UnPauseBird(){
		if(!dead){
			anim.speed = originalAnimSpeed;
			GetComponent<Rigidbody>().isKinematic = false;
			GetComponent<Rigidbody>().linearVelocity = originalVelocity;
			paused = false;
		}
	}
	
	public IEnumerator FlyToTarget(Vector3 target)
    {
        // Audio
        var audio = GetComponent<AudioSource>();
        if (audio != null)
        {
            audio.PlayOneShot(Random.value < 0.5f ? flyAway1 : flyAway2, 0.1f);
        }

        // State
        flying = true;
        landing = false;
        onGround = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
        }

        anim.applyRootMotion = false;
        anim.SetBool(flyingBoolHash, true);
        anim.SetBool(landingBoolHash, false);

        // Wait until flying animation starts
        while (!anim.GetCurrentAnimatorStateInfo(0).IsName("fly"))
            yield return null;

        Vector3 startPos = transform.position;
        Vector3 endPos = target;

        float duration = 2.2f; // flight speed
        float height = 1f * controller.birdScale;
        float t = 0f;

        // --- FLIGHT ---
        while (t < 1f)
        {
            if (!paused)
            {
                t += Time.deltaTime / duration;

                // Smooth arc
                Vector3 pos = Vector3.Lerp(startPos, endPos, t);
                pos.y += Mathf.Sin(t * Mathf.PI) * height;
                transform.position = pos;

                // Direction toward movement (XZ plane)
                Vector3 moveDir = (endPos - transform.position);
                moveDir.y = 0f;

                Quaternion targetRot;

                if (moveDir.sqrMagnitude > 0.001f)
                {
                    // Rotation following movement direction
                    Quaternion moveRot = Quaternion.LookRotation(moveDir);

                    // Rotation facing the camera
                    Vector3 cameraDir = Camera.main.transform.position - transform.position;
                    cameraDir.y = 0f;
                    Quaternion cameraRot = Quaternion.LookRotation(cameraDir) * Quaternion.Euler(0f, 180f, 0f); // fix model orientation

                    // Blend both rotations (weight controls how much the bird faces camera)
                    float cameraBlend = 0.3f; // 0 = fully movement, 1 = fully camera
                    targetRot = Quaternion.Slerp(moveRot, cameraRot, cameraBlend);
                }
                else
                {
                    targetRot = transform.rotation;
                }

                // Smoothly apply rotation
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 6f);
            }
            yield return null;
        }

        // --- LANDING ---
        flying = false;
        landing = true;

        anim.SetBool(flyingBoolHash, false);
        anim.SetBool(landingBoolHash, true);

        // Face camera on Y
        if (Camera.main != null)
        {
            Vector3 camDir = -Camera.main.transform.forward;
            camDir.y = 0f;

            if (camDir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(camDir);
        }

        // Smooth final snap
        Vector3 velocity = Vector3.zero;
        float landTime = 0.3f;
        float landT = 0f;

        while (landT < landTime)
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                target,
                ref velocity,
                landTime
            );

            landT += Time.deltaTime;
            yield return null;
        }

        // Finalize
        transform.position = target;
        anim.SetBool(landingBoolHash, false);

        anim.applyRootMotion = true;
        onGround = true;
        landing = false;
    }


	//Sets a variable between -1 and 1 to control the left and right banking animation
	float FindBankingAngle(Vector3 birdForward, Vector3 dirToTarget){
		Vector3 cr = Vector3.Cross (birdForward,dirToTarget);
		float ang = Vector3.Dot (cr,Vector3.up);
		return ang;
	}
	
	void OnGroundBehaviors(){
		idle = anim.GetCurrentAnimatorStateInfo(0).nameHash == idleAnimationHash;
		if(!GetComponent<Rigidbody>().isKinematic){
			GetComponent<Rigidbody>().isKinematic = true;
		}
		if (idle)
        {
            if (Random.value < Time.deltaTime * .33f)
            {
                float rand = Random.value;

                if (rand < .3f)
                {
                    DisplayBehavior(birdBehaviors.sing);
                }
                else if (rand < .5f)
                {
                    DisplayBehavior(birdBehaviors.peck);
                }
                else if (rand < .6f)
                {
                    DisplayBehavior(birdBehaviors.preen);
                }
                else if (!perched && rand < .7f)
                {
                    DisplayBehavior(birdBehaviors.ruffle);
                }
                else if (!perched && rand < .8f && canHopForward)
                {
                    DisplayBehavior(birdBehaviors.hopForward);
                    canHopForward = false;
                    canHopBackward = true;
                }
                else if (!perched && rand < .9f && canHopLeft)
                {
                    DisplayBehavior(birdBehaviors.hopLeft);
                    canHopLeft = false;
                    canHopRight = true;
                }
                else if (!perched && rand < .95f && canHopRight)
                {
                    DisplayBehavior(birdBehaviors.hopRight);
                    canHopRight = false;
                    canHopLeft = true;
                }
                else if (!perched && rand <= 1f && canHopBackward)
                {
                    DisplayBehavior(birdBehaviors.hopBackward);
                    canHopBackward = false;
                    canHopForward = true;
                }
                else
                {
                    // Fallback to a safe idle action
                    DisplayBehavior(birdBehaviors.sing);
                }

                anim.SetFloat("IdleAgitated", Random.value);
            }
        }

	}
	
	void DisplayBehavior(birdBehaviors behavior){
		idle = false;
		switch (behavior){
		case birdBehaviors.sing:
			anim.SetTrigger(singTriggerHash);			
			break;
		case birdBehaviors.ruffle:
			anim.SetTrigger(ruffleBoolHash);
			break;
		case birdBehaviors.preen:
			anim.SetTrigger(preenBoolHash);			
			break;
		case birdBehaviors.peck:
			anim.SetTrigger(peckBoolHash);			
			break;
		case birdBehaviors.hopForward:
			anim.SetInteger (hopIntHash, 1);			
			break;
		case birdBehaviors.hopLeft:
			anim.SetInteger (hopIntHash, -2);			
			break;
		case birdBehaviors.hopRight:
			anim.SetInteger (hopIntHash, 2);
			break;
		case birdBehaviors.hopBackward:
			anim.SetInteger (hopIntHash, -1);			
			break;
		}
	}

	void OnTriggerEnter(Collider col){
		if (col.tag == "lb_bird"){
			// FlyAway ();
		}
	}

	void OnTriggerExit(Collider col){
		//if bird has hopped out of the target area lets fly
		if (onGround && (col.tag == "lb_groundTarget" || col.tag == "lb_perchTarget")){
			// FlyAway ();
		}
	}

	void AbortFlyToTarget(){
		StopCoroutine("FlyToTarget");
		solidCollider.enabled = false;
		anim.SetBool(landingBoolHash, false);
		anim.SetFloat (flyingDirectionHash,0);
		transform.localEulerAngles = new Vector3(
			0.0f,
			transform.localEulerAngles.y,
			0.0f);
		FlyAway ();
	}
	
	void FlyAway(){
		if(!dead){
			StopCoroutine("FlyToTarget");
			anim.SetBool(landingBoolHash, false);
			controller.SendMessage ("BirdFindTarget",gameObject);
		}
	}

	void Flee(){
		if(!dead){
			StopCoroutine("FlyToTarget");
			GetComponent<AudioSource>().Stop();
			anim.Play(flyAnimationHash);
			Vector3 farAwayTarget = transform.position;
			farAwayTarget += new Vector3(Random.Range (-100,100)*controller.birdScale,10*controller.birdScale,Random.Range (-100,100)*controller.birdScale);
			StartCoroutine("FlyToTarget",farAwayTarget);
		}
	}

	void CrowIsClose(){
		if (fleeCrows && !dead){
			Flee ();
		}
	}

	public void KillBird(){
		if(!dead){
			controller.SendMessage ("FeatherEmit",transform.position);
			anim.SetTrigger(dieTriggerHash);
			anim.applyRootMotion = false;
			dead = true;
			onGround = false;
			flying = false;
			landing = false;
			idle = false;
			perched = false;
			AbortFlyToTarget();
			StopAllCoroutines();
			GetComponent<Collider>().isTrigger = false;
			birdCollider.center = new Vector3(0.0f,0.0f,0.0f);
			birdCollider.size = new Vector3(0.1f,0.01f,0.1f)*controller.birdScale;
			GetComponent<Rigidbody>().isKinematic = false;
			GetComponent<Rigidbody>().useGravity = true;
		}
	}

	public void KillBirdWithForce(Vector3 force){
		if(!dead){
			controller.SendMessage ("FeatherEmit",transform.position);
			anim.SetTrigger(dieTriggerHash);
			anim.applyRootMotion = false;
			dead = true;
			onGround = false;
			flying = false;
			landing = false;
			idle = false;
			perched = false;
			AbortFlyToTarget();
			StopAllCoroutines();
			GetComponent<Collider>().isTrigger = false;
			birdCollider.center = new Vector3(0.0f,0.0f,0.0f);
			birdCollider.size = new Vector3(0.1f,0.01f,0.1f)*controller.birdScale;
			GetComponent<Rigidbody>().isKinematic = false;
			GetComponent<Rigidbody>().useGravity = true;
			GetComponent<Rigidbody>().AddForce (force);
		}
	}

	void Revive(){
		if(dead){
			birdCollider.center = bColCenter;
			birdCollider.size = bColSize;
			GetComponent<Collider>().isTrigger = true;
			dead = false;
			onGround = false;
			flying = false;
			landing = false;
			idle = true;
			perched = false;
			GetComponent<Rigidbody>().isKinematic = false;
			GetComponent<Rigidbody>().useGravity = false;
			anim.Play (idleAnimationHash);
			controller.SendMessage ("BirdFindTarget",gameObject);
		}
	}

	void SetController(lb_BirdController cont){
		controller = cont;
	}

	void ResetHopInt(){
		anim.SetInteger (hopIntHash, 0);
	}

	void ResetFlyingLandingVariables(){
		if (flying || landing){
			flying = false;
			landing = false;
		}
	}

	void PlaySong(){
		if (!dead){
			if(Random.value < .5){
				GetComponent<AudioSource>().PlayOneShot (song1,1);
			}else{
				GetComponent<AudioSource>().PlayOneShot (song2,1);
			}
		}
	}

	void Update () {
		if(onGround && !paused && !dead){
			OnGroundBehaviors();	
		}
	}
}

﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerMovingState
{
    Iddle, Moving, Jumping
}

public class PlayerMovement : MonoBehaviour
{
    //Collisions
    private const float shellRadious = 0.1f;

    private enum PlayerState
    {
        Grounded, Air, Dash
    }

    private PlayerState ps;

    public float MoveX;
    public bool red;

    public bool FacingRight = true;
    public bool Active = true;

    public PlayerMovingState MovingState {
        get {
            return movingState;
        }
    }

    //Input
    private bool jumpPressed = false;
    private int moveAxis = 0;
    private int yAxis = 0;

    private Rigidbody2D coll;
    private ContactFilter2D cf;

    private PlayerMovingState movingState;

    private ContactFilter2D triggers;
    private Collider2D[] triggered;

    private float timeJump = 0.4f, heightJump = 3.5f;
    private float gravity, jump;
    private float vy = 0;

    //Dash movement
    private float dashCounter = 0.0f;
    private const float dashTime = 0.25f;
    private int dashX, dashY;
    private const float dashSpeed = 16.0f;

    private HashSet<Collider2D> ignoreColliders;
    // Start is called before the first frame update
    void Start()
    {
        coll = gameObject.GetComponent<Rigidbody2D>();
        cf = new ContactFilter2D();
        cf.NoFilter();
        red = true;
        ignoreColliders = new HashSet<Collider2D>();

        jump = 4 * heightJump / timeJump;
        gravity = 2 * jump / timeJump;

        triggered = new Collider2D[4];

        ps = PlayerState.Grounded;

        triggers = new ContactFilter2D();
        triggers.NoFilter();
        triggers.SetLayerMask(LayerMask.GetMask("Trigger"));
    }

    private void SetMask(bool platform)
    {
        int r = LayerMask.GetMask("Solid") | LayerMask.GetMask("Color1");
        int b = LayerMask.GetMask("Solid") | LayerMask.GetMask("Color2");
        if(platform)
        {
            r |= LayerMask.GetMask("PlatformColor1");
            b |= LayerMask.GetMask("PlatformColor2");
        }


        cf.SetLayerMask(red ? r : b);
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftArrow)) moveAxis = -1;
        else if (Input.GetKey(KeyCode.RightArrow)) moveAxis = 1;

        if (Input.GetKey(KeyCode.UpArrow)) yAxis = 1;
        else if (Input.GetKey(KeyCode.DownArrow)) yAxis = -1;

        if (moveAxis > 0) FacingRight = true;
        else if (moveAxis < 0) FacingRight = false;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpPressed = true;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Active)
        {
            switch (ps)
            {
                case PlayerState.Grounded:
                    GroundedMovement();
                    break;
                case PlayerState.Air:
                    AirMovement();
                    break;
                case PlayerState.Dash:
                    DashMovement();
                    break;
            }

            int r = coll.OverlapCollider(triggers, triggered);
            for (int i = 0; i < r; i++)
            {
                TriggerAction s = triggered[i].gameObject.GetComponent<TriggerAction>();
                if (s)
                {
                    s.PlayerTriggered(this.gameObject.GetComponent<Player>());
                }

            }

            moveAxis = 0;
            yAxis = 0;
            jumpPressed = false;
        }
    }

    void AirMovement()
    {
        float vx = MoveX * moveAxis;

        if (jumpPressed && (moveAxis != 0 || yAxis != 0))
        {
            dashCounter = dashTime;
            ps = PlayerState.Dash;
            dashX = moveAxis;
            dashY = yAxis;
        }

        vy -= gravity * Time.fixedDeltaTime;

        if (vy < -14) vy = -14;

        bool c = MoveY(vy * Time.fixedDeltaTime);
        MovX(vx * Time.fixedDeltaTime);
        if (c && vy < 0)
        {
            ps = PlayerState.Grounded;
        }
    }

    void DashMovement()
    {
        if (dashCounter < 0)
        {
            ps = PlayerState.Grounded;
        } else
        {
            dashCounter -= Time.fixedDeltaTime;

            Vector2 m = new Vector2(dashX, dashY).normalized * dashSpeed;

            bool hit = MovX(m.x * Time.fixedDeltaTime);

            if (hit && dashY == 0)
            {
                vy = 0;
                ps = PlayerState.Air;
            }

            hit = MoveY(m.y * Time.fixedDeltaTime);
            if (hit)
            {
                if (m.y > 0 && dashX == 0)
                {
                    ps = PlayerState.Air;
                    vy = dashSpeed;
                }
                else if (dashX == 0)
                {
                    ps = PlayerState.Grounded;
                }
            }
        }
    }

    void GroundedMovement()
    {
        float vx = MoveX * moveAxis;

        if (jumpPressed)
        {
            vy = jump;
            ps = PlayerState.Air;
        } else
        {
            vy = -14;
        }

        bool c = MoveY(vy * Time.fixedDeltaTime);
        MovX(vx * Time.fixedDeltaTime);
        if (!c)
        {
            ps = PlayerState.Air;
        }

        movingState = PlayerMovingState.Jumping;
    }

    bool MovX(float delta)
    {
        SetMask(false);

        bool ret = false;
        float shellDIst = shellRadious;
        ignoreColliders.Clear();

        Collider2D[] ov = new Collider2D[4];

        int r = coll.OverlapCollider(cf, ov);

        for(int i = 0; i < r; i++)
        {
            ignoreColliders.Add(ov[i]);
        }

        Vector3 desp = new Vector3(delta, 0, 0);

        RaycastHit2D[] res = new RaycastHit2D[6];

        r = coll.Cast(desp.normalized, cf, res, desp.magnitude + shellDIst);

        if (r == 0)
        {
            coll.transform.position = coll.transform.position + new Vector3(delta, 0, 0);
            return false;
        }

        float sep = Mathf.Abs(delta);

        for (int i = 0; i < r; i++)
        {
            RaycastHit2D rc = res[i];
            if (ignoreColliders.Contains(rc.collider)) continue;

            ret = true;

            float distance = rc.distance - shellDIst;

            if (distance < 0) distance = 0;

            if (distance < sep) sep = distance;
        }

        coll.transform.position = coll.transform.position + desp.normalized * sep;

        return ret;
    }

    bool MoveY(float delta)
    {
        SetMask(delta < 0);
        bool ret = false;
        float shellDIst = shellRadious;
        ignoreColliders.Clear();

        Collider2D[] ov = new Collider2D[4];

        int r = coll.OverlapCollider(cf, ov);

        for (int i = 0; i < r; i++)
        {
            ignoreColliders.Add(ov[i]);
        }

        Vector3 desp = new Vector3(0, delta, 0);

        RaycastHit2D[] res = new RaycastHit2D[6];

        r = coll.Cast(desp.normalized, cf, res, desp.magnitude + shellDIst);

        if (r == 0)
        {
            coll.transform.position = coll.transform.position + new Vector3(0, delta, 0);
            return false;
        }

        float sep = Mathf.Abs(delta);

        for (int i = 0; i < r; i++)
        {
            RaycastHit2D rc = res[i];
            if (ignoreColliders.Contains(rc.collider))
            {
                continue;
            }
            ret = true;

            float distance = rc.distance - shellDIst;
            if (distance < 0) distance = 0;

            if (distance < sep) sep = distance;
        }

        coll.transform.position = coll.transform.position + desp.normalized * sep;

        return ret;
    }
}

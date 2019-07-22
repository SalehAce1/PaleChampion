using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PaleChampion
{ 
    public class HomingInfection : MonoBehaviour
    {
        private float RotateSpeed = 5f;
        private float Radius = 3.5f;

        public Transform centre;
        private float _angle;
        Rigidbody2D rb;
        float time = 0;
        Transform target = HeroController.instance.transform;
        float speed = 18f;

        private void Start()
        {
            rb = gameObject.GetComponent<Rigidbody2D>();
        }
        private bool _once;
        private void FixedUpdate()
        {
            if (time < 5f)
            {
                _angle += RotateSpeed * Time.deltaTime;
                var offset = new Vector2(Mathf.Sin(_angle), Mathf.Cos(_angle)) * Radius;
                transform.position = (Vector2) centre.position + offset;
            }
            else if (time < 5.2f)
            {
                var p1 = gameObject.transform.position;
                Vector3 vectorToTarget = target.position - p1;
                float angle2 = Mathf.Atan2(vectorToTarget.y, vectorToTarget.x) * Mathf.Rad2Deg;
                float angle = angle2 * Mathf.Deg2Rad;
                Quaternion q = Quaternion.AngleAxis(angle2, Vector3.forward);
                gameObject.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle2));//Quaternion.Slerp(spike.transform.rotation, q, Time.deltaTime * 1000f);
                rb.velocity = new Vector2(speed * Mathf.Cos(angle), speed * Mathf.Sin(angle));
            }
            if (time > 5f && !_once)
            {
                AspidControl.liveOrbs.Remove(gameObject);
                _once = true;
            }
            time += Time.deltaTime;
        }
        
    }
}

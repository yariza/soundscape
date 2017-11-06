using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Metronome : MonoBehaviour {

    private static Metronome instance;

    public int BPM = 120;
    public int divisions = 2;

    public delegate void OnBeat();
    public OnBeat onBeat;

    private bool metronomeStarted = false;

    public static Metronome Instance
    {
        get { return instance; }
    }

    public float BeatInSeconds
    {
        get { return 60f / (float)(BPM * divisions); }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
    
    void Start () {
		if (!metronomeStarted)
        {
            metronomeStarted = true;

            StartCoroutine(Tick());
        }
	}

    IEnumerator Tick()
    {
        for (;;)
        {
            if (onBeat != null)
            {
                onBeat();
            }

            yield return new WaitForSeconds(60f / (float)(BPM * divisions));
        }
    }
}

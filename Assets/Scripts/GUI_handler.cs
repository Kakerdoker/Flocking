using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUI_handler : MonoBehaviour
{
    public Text timer, greenGuysText, console;
    public main main;
    public bool consoleOn = false;
    string consoleInput = "";
    int amountToAdd = 0, score;

    float timeClock = 300;
    float nextRedSpawn = 300;

    void calculateAndSetTime(){
        if(timeClock > 0.1f){
            score = main.greenGuys;
            greenGuysText.text = "Don't let the green guys get eaten! \nThere are only " + score.ToString() + " left!";
            timeClock-= Time.deltaTime;

            if(nextRedSpawn > timeClock){
                nextRedSpawn -= 30f;
                main.addEntities(1,1);
            }
            int timeClockInt = (int)timeClock;
            int seconds = timeClockInt % 60;
            int minutes = (timeClockInt / 60) % 60;

            string strSeconds = seconds > 9 ? seconds.ToString() : '0' + seconds.ToString();
            string strMinutes = minutes > 9 ? minutes.ToString() : '0' + minutes.ToString();

            timer.text = strMinutes + ':' + strSeconds;
        }
        else{  
            greenGuysText.transform.localPosition = new Vector3(-381.4f,94f,0f);
            greenGuysText.text = "Score: " + score.ToString();
            greenGuysText.alignment = TextAnchor.MiddleCenter;
        }
    }

    void addEntitiesCommand(int type){
        int.TryParse(consoleInput.Split(' ')[2], out amountToAdd);
        //amountToAdd = System.Int32.Parse();
        if(amountToAdd > 0 && amountToAdd < 5000){
            main.addEntities(amountToAdd, type);
            consoleOn = false;
            console.text = consoleInput = "";
        }
        else{
            console.text = consoleInput = "ERR";
        }
    }

    void whichLetter(string letter)
    {
        switch (letter)
        {
            case "RETURN":
                if(consoleInput.StartsWith("ADD GREEN ")){
                    addEntitiesCommand(0);
                }
                else if(consoleInput.StartsWith("ADD RED" )){
                    addEntitiesCommand(1);
                }
                else{
                    consoleOn = false;
                    console.text = consoleInput = "";
                }
                break;
            case "BACKSPACE":
                if (consoleInput.Length != 0) {
                    consoleInput = consoleInput.Remove(consoleInput.Length - 1);
                }
                break;
            case "SPACE":
                if(consoleInput.Length <= 32){
                    consoleInput += " ";
                }
                break;
            default:
                if(consoleInput.Length <= 32){
                    consoleInput += letter;
                }
            break;
        }
    }

    void Start()
    {
        
    }

    string[] letters = {"q","w","e","r","t","y","u","i","o","p","a","s","d","f","g","h","j","k","l","z","x","c","v","b","n","m", "space","backspace","return","0","1","2","3","4","5","6","7","8","9"};

    // Update is called once per frame
    void Update()
    {
        calculateAndSetTime();
        
        if(Input.GetKeyDown("`")){
            consoleOn = !consoleOn;
            console.text = consoleInput = "";
        }
        if(consoleOn){
            foreach (string letter in this.letters)
            {
                if (Input.GetKeyDown(letter))
                {
                    whichLetter(letter.ToUpper());
                    console.text = consoleInput;
                }
            } 
        }
        
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stats : MonoBehaviour {

    private GeneticAlgorithm GA;

    public string FileName;
    public string FolderName;
    public string BotFileName;

    public bool OnDesktop;
    public bool InAppFolder;
    public bool InFolder;

    string data = "";
    string botData = "";

    string AppPath;

    void Start () {
        GA = gameObject.GetComponent<GeneticAlgorithm>();
        AppPath = Application.dataPath;
    }

    public void NewStatFile()
    {
        data = "Generation;Meilleur score;Score moyen;Robots en vie;Robots morts;Meilleur distance parcourue (m);Distance moyenne parcourue (m);Meilleur duree de vie (s);Duree de vie moyenne (s);Meilleur vitesse moyenne (m/s);Vitesse moyenne (m/s)";
        botData = "";
    }

    public void SaveGenerationStats ()
    {
        long scoreSum = 0;
        float distSum = 0f;
        float lifetimeSum = 0f;
        float meanSpeedSum = 0f;
        int alivebot = 0;
        int deadbot = 0;
        foreach (Robot rob in GA.population)
        {
            scoreSum += rob.score;
            distSum += rob.TotalDist;
            lifetimeSum += rob.LifeTime;
            meanSpeedSum += rob.MeanSpeed;
            if (rob.alive)
            {
                alivebot++;
            } else
            {
                deadbot++;
            }
        }
        long meanScore = scoreSum / (long)GA.populationSize;
        float meanDist = distSum / GA.populationSize;
        float meanLifetime = lifetimeSum / GA.populationSize;
        float MeanmeanSpeed = meanSpeedSum / GA.populationSize;
    
        string Line = "\n" + GA.generation.ToString() + ";"
                    + GA.bestRobot.score.ToString() + ";"
                    + meanScore.ToString() + ";"
                    + alivebot.ToString() + ";"
                    + deadbot.ToString() + ";"
                    + (GA.bestRobot.TotalDist / 10.0f).ToString() + ";"
                    + (meanDist / 10.0f).ToString() + ";"
                    + GA.bestRobot.LifeTime.ToString() + ";"
                    + meanLifetime.ToString() + ";"
                    + (GA.bestRobot.MeanSpeed / 10.0f).ToString() + ";"
                    + (MeanmeanSpeed / 10.0f).ToString();    
        
        Line = Line.Replace(".", ",");
        data += Line;
    }

    public void Save ()
    {
        Robot robToSave = null;

        if (GetComponent<AppManager>().SelectedRob != null)
        {
            robToSave = GetComponent<AppManager>().SelectedRob;
        }
        else
        {
            robToSave = GA.bestRobot;
        }

        botData += GA.generation.ToString();
        botData += "\r\n" + robToSave.input.ToString();
        botData += "\r\n" + robToSave.hidden.ToString();
        botData += "\r\n" + robToSave.output.ToString();
        float[] weights = robToSave.brain.toDataArray();
        foreach(float val in weights)
        {
            botData += "\r\n" + val.ToString();
        }


        if (FileName != "")
        {
            string Date = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

            if (InFolder)
            {
                if (FolderName != "")
                {
                    if (InAppFolder)
                    {
                        if (!System.IO.Directory.Exists(AppPath + "\\" + FolderName))
                        {
                            System.IO.Directory.CreateDirectory(AppPath + "\\" + FolderName);
                        }
                        System.IO.File.WriteAllText(AppPath + "\\" + FolderName + "\\" + FileName + "_" + Date + ".csv", data);
                        System.IO.File.WriteAllText(AppPath + "\\" + FolderName + "\\" + BotFileName + "_" + Date + ".txt", botData);
                    }
                    if (OnDesktop)
                    {
                        if (!System.IO.Directory.Exists(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop) + "\\" + FolderName))
                        {
                            System.IO.Directory.CreateDirectory(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop) + "\\" + FolderName);
                        }
                        System.IO.File.WriteAllText(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop) + "\\" + FolderName + "\\" + FileName + "_" + Date + ".csv", data);
                        System.IO.File.WriteAllText(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop) + "\\" + FolderName + "\\" + BotFileName + "_" + Date + ".txt", botData);
                    }
                }
            }
            else
            {
                if (InAppFolder)
                {
                    System.IO.File.WriteAllText(FileName + "_" + Date + ".csv", data);
                    System.IO.File.WriteAllText(BotFileName + "_" + Date + ".txt", botData);
                }
                if (OnDesktop)
                {
                    System.IO.File.WriteAllText(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop) + "\\" + FileName + "_" + Date + ".csv", data);
                    System.IO.File.WriteAllText(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop) + "\\" + BotFileName + "_" + Date + ".txt", botData);
                }
            }
        }
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagement : MonoBehaviour
{
    //Education Mode
    public void Home()
    {
        SceneManager.LoadScene("Home (Dashboard)");
    }

    public void News()
    {
        SceneManager.LoadScene("Pusat Berita");
    }

    public void Company()
    {
        SceneManager.LoadScene("Perusahaan");
    }

    public void Market()
    {
        SceneManager.LoadScene("Pasar Saham");
    }

    public void Glosarium()
    {
        SceneManager.LoadScene("Glosarium");
    }

    public void ExitGame()
    {
        SceneManager.LoadScene("Menu");
    }

    public void Dashboard()
    {
        SceneManager.LoadScene("Home (Dashboard)");
    }

    public void Lesson()
    {
        SceneManager.LoadScene("Home (Pelajaran)");
    }

    public void CandlestickLesson()
    {
        SceneManager.LoadScene("Home (Candlestick)");
    }

    public void Quiz()
    {
        SceneManager.LoadScene("Home (Kuis)");
    }

    //GameMode
    public void GameWatchlist()
    {
        SceneManager.LoadScene("GameMode");
    }

    public void GameMarket()
    {
        SceneManager.LoadScene("GameMode (Market)");
    }

    public void GamePortofolio()
    {
        SceneManager.LoadScene("GameMode (Portofolio)");
    }

    public void GameNews()
    {
        SceneManager.LoadScene("GameMode (World News)");
    }
}

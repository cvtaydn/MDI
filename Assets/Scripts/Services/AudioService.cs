using UnityEngine;

/// <summary>
/// Audio service implementation for managing game audio
/// </summary>
public class AudioService : IAudioService
{
    private float _masterVolume = 1.0f;
    private float _musicVolume = 1.0f;
    private float _sfxVolume = 1.0f;
    private string _currentBackgroundMusic;

    public void PlayBackgroundMusic(string musicName)
    {
        _currentBackgroundMusic = musicName;
        Debug.Log($"[AudioService] Playing background music: {musicName} (Volume: {_musicVolume * _masterVolume:P0})");
        // Audio clip loading ve playing logic'i burada olacak
    }

    public void PlaySound(string soundName)
    {
        Debug.Log($"[AudioService] Playing sound: {soundName} (Volume: {_sfxVolume * _masterVolume:P0})");
        // Sound effect playing logic'i burada olacak
    }

    public void StopBackgroundMusic()
    {
        Debug.Log($"[AudioService] Stopping background music: {_currentBackgroundMusic}");
        _currentBackgroundMusic = null;
        // Background music'i durdur
    }

    public void SetVolume(float volume)
    {
        _masterVolume = Mathf.Clamp01(volume);
        Debug.Log($"[AudioService] Master volume set to: {_masterVolume:P0}");
        // Master volume ayarlama logic'i burada olacak
    }

    public void SetMusicVolume(float volume)
    {
        _musicVolume = Mathf.Clamp01(volume);
        Debug.Log($"[AudioService] Music volume set to: {_musicVolume:P0}");
        // Music volume ayarlama logic'i burada olacak
    }

    public void SetSFXVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        Debug.Log($"[AudioService] SFX volume set to: {_sfxVolume:P0}");
        // SFX volume ayarlama logic'i burada olacak
    }

    public void Initialize()
    {
        Debug.Log("[AudioService] Audio Service initialized");
        SetVolume(1.0f);
        SetMusicVolume(0.8f);
        SetSFXVolume(1.0f);
    }

    public void Cleanup()
    {
        Debug.Log("[AudioService] Audio Service cleanup completed");
        StopBackgroundMusic();
    }
}
using UnityEngine;

/// <summary>
/// Audio service interface for managing game audio
/// </summary>
public interface IAudioService
{
    void PlayBackgroundMusic(string musicName);
    void PlaySound(string soundName);
    void StopBackgroundMusic();
    void SetVolume(float volume);
    void SetMusicVolume(float volume);
    void SetSFXVolume(float volume);
    void Initialize();
    void Cleanup();
}
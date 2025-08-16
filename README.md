# MDI+ (Modular Dependency Injection Plus)

Unity için gelişmiş, pattern-based, signal-driven, command-oriented dependency injection sistemi.

## 🚀 Özellikler

- **SOLID Principles**: Clean architecture ve SOLID prensiplerine uygun
- **Easy to Use**: Attribute-based injection, fluent API
- **Pattern Support**: Signal system, Command pattern, Function pipeline
- **Unity Integration**: MonoBehaviour, ScriptableObject desteği
- **Performance**: Optimized memory management, fast resolution
- **Extensible**: Plugin sistemi ile genişletilebilir

## 📦 Kurulum

### Yöntem 1: Unity Package Manager ile (Git URL)
1. Unity'de `Window > Package Manager` açın
2. `+` butonuna tıklayın
3. `Add package from git URL` seçin
4. `https://github.com/cvtaydn/MDI.git` girin

**Not:** Git URL ile ekleme yapamazsanız aşağıdaki alternatif yöntemleri kullanın.

### Yöntem 2: Manuel Package Kurulumu
1. Bu repository'yi indirin (Download ZIP)
2. ZIP dosyasını açın
3. `Package` klasörünü Unity projenizin `Packages` klasörüne kopyalayın
4. Unity'yi yeniden başlatın

### Yöntem 3: Manifest.json ile
1. Unity projenizin `Packages/manifest.json` dosyasını açın
2. `dependencies` bölümüne şu satırı ekleyin:
```json
"com.mdi.core": "https://github.com/cvtaydn/MDI.git"
```
3. Unity'yi yeniden başlatın

### Yöntem 4: Local Package
1. Bu repository'yi clone edin veya indirin
2. Unity Package Manager'da `+` > `Add package from disk` seçin
3. İndirdiğiniz klasördeki `Package/package.json` dosyasını seçin

## 🎯 Hızlı Başlangıç

### 1. Basit Kullanım
```csharp
// Container oluştur
using var container = new MDIContainer();

// Service'leri register et
container
    .RegisterSingleton<ILogger, ConsoleLogger>()
    .RegisterTransient<IEmailService, EmailService>();

// Service'leri resolve et
var logger = container.Resolve<ILogger>();
var emailService = container.Resolve<IEmailService>();
```

### 2. Unity Integration
```csharp
public class GameManager : MonoBehaviour
{
    [Inject] private IGameService gameService;
    [Inject] private IAudioService audioService;
    
    private void Awake()
    {
        MDI.Inject(this);
    }
}
```

### 3. Signal System
```csharp
// Signal tanımla
public class PlayerDeathSignal : ISignal
{
    public Vector3 DeathPosition { get; set; }
}

// Signal gönder
SignalBus.Publish(new PlayerDeathSignal { 
    DeathPosition = transform.position 
});

// Signal dinle
SignalBus.Subscribe<PlayerDeathSignal>(OnPlayerDeath);
```

### 4. Command Pattern
```csharp
// Command'ları sırayla çalıştır
CommandExecutor.ExecuteSequentially(
    new MovePlayerCommand { TargetPosition = new Vector3(10, 0, 0) },
    new PlayAnimationCommand { AnimationName = "Walk" }
);
```

## 🏗️ Mimari

### Core DI System
- **Container Types**: Singleton, Transient, Scoped, Lazy
- **Lifecycle Management**: Unity event entegrasyonu
- **Validation**: Circular dependency detection
- **Performance**: Fast resolution, memory pooling

### Pattern System
- **Signal System**: Event-driven communication
- **Command Pattern**: Sequential, Parallel, Conditional execution
- **Function Pipeline**: Data transformation chains
- **Observer Pattern**: Reactive programming support

### Unity Integration
- **MonoBehaviour Support**: Automatic injection
- **ScriptableObject Support**: Configuration injection
- **Prefab Injection**: Instant injection on creation
- **Scene Scoping**: Scene-based service lifetime

## 📚 Dokümantasyon

- [API Reference](Documentation/API.md)
- [Examples](Examples/)
- [Best Practices](Documentation/BestPractices.md)
- [Performance Guide](Documentation/Performance.md)

## 🧪 Test

```bash
# Unit testleri çalıştır
dotnet test Tests/Unit

# Integration testleri çalıştır
dotnet test Tests/Integration
```

## 🤝 Katkıda Bulunma

1. Fork edin
2. Feature branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Commit edin (`git commit -m 'Add amazing feature'`)
4. Push edin (`git push origin feature/amazing-feature`)
5. Pull Request oluşturun

## 📄 Lisans

Bu proje MIT lisansı altında lisanslanmıştır. Detaylar için [LICENSE](LICENSE) dosyasına bakın.

## 🆘 Destek

- [Issues](https://github.com/cvtaydn/MDI/issues)
- [Discussions](https://github.com/cvtaydn/MDI/discussions)
- [Wiki](https://github.com/cvtaydn/MDI/wiki)

## 🎯 Roadmap

- [x] Core DI System
- [x] Basic Unity Integration
- [ ] Signal System
- [ ] Command Pattern
- [ ] Function Pipeline
- [ ] Advanced Unity Features
- [ ] Performance Optimization
- [ ] Visual Debug Tools

---

**MDI+** - Unity'de dependency injection'ın geleceği! 🚀

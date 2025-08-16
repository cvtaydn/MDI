# MDI+ (Modular Dependency Injection Plus)

Unity için gelişmiş, pattern-based, signal-driven, command-oriented dependency injection sistemi.

## 🚀 Özellikler

- **SOLID Principles**: Clean architecture ve SOLID prensiplerine uygun
- **Easy to Use**: Attribute-based injection, fluent API, helper metodlar
- **Advanced Features**: Real-time validation, dependency monitoring, health checks
- **Unity Integration**: MonoBehaviour, ScriptableObject, Editor tools desteği
- **Performance**: Optimized memory management, fast resolution, performance monitoring
- **Developer Tools**: Visual debugging, setup wizard, code generation
- **Extensible**: Plugin sistemi ile genişletilebilir

## 📦 Kurulum

### Yöntem 1: Unity Package Manager ile (Git URL)
1. Unity'de `Window > Package Manager` açın
2. `+` butonuna tıklayın
3. `Add package from git URL` seçin
4. `https://github.com/cvtaydn/MDI.git` girin

**Not:** Artık package.json dosyası root seviyesinde olduğu için Git URL ile ekleme çalışmalıdır. Sorun yaşarsanız aşağıdaki alternatif yöntemleri kullanın.

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
3. İndirdiğiniz klasördeki root seviyesindeki `package.json` dosyasını seçin

## 🎯 Hızlı Başlangıç

### 1. Setup Wizard ile Kurulum
```csharp
// Unity Editor'de: Tools > MDI+ > Setup Wizard
// Otomatik kurulum ve konfigürasyon
```

### 2. Basit Kullanım
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

### 3. Fluent API ile Gelişmiş Kullanım
```csharp
// Configuration ile
MDIConfiguration.ConfigureGlobalServices(config =>
{
    config.Configure<ILogger, ConsoleLogger>()
          .AsSingleton()
          .WithName("MainLogger")
          .InDebugOnly();
          
    config.Configure<IEmailService, EmailService>()
          .AsTransient()
          .When(() => Application.isPlaying);
});

// Helper metodlar ile
MDI.UseService<ILogger>(logger => logger.Log("Hello World!"));
MDI.IfServiceExists<IEmailService>(email => email.SendWelcomeEmail());
```

### 4. Unity Integration
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

// Auto-registration ile
[MDIAutoRegister(ServiceLifetime.Singleton)]
public class PlayerService : IPlayerService
{
    // Otomatik olarak register edilir
}
```

### 5. Validation ve Monitoring
```csharp
// Real-time validation
var validator = new MDIDependencyValidator();
var issues = validator.ValidateAllDependencies();

// Performance monitoring
var report = MDI.GetPerformanceReport();
Debug.Log(report);

// Health check
if (MDI.IsHealthy())
{
    Debug.Log("All services are healthy!");
}
```

### 6. Editor Tools
```csharp
// Unity Editor'de:
// - Tools > MDI+ > Monitor Window (service monitoring)
// - Tools > MDI+ > Validation Window (dependency validation)
// - Tools > MDI+ > Service Generator (code generation)
// - Inspector'da service'leri görsel yönetim
```

## 🏗️ Mimari

### Core DI System
- **Container Types**: Singleton, Transient, Scoped, Factory, Instance
- **Lifecycle Management**: Unity event entegrasyonu, otomatik cleanup
- **Validation**: Circular dependency detection, real-time monitoring
- **Performance**: Fast resolution, memory pooling, performance metrics
- **Helper Methods**: TryResolve, UseService, IfServiceExists, ResolveMultiple

### Advanced Features
- **Auto-Registration**: Attribute-based service discovery
- **Fluent Configuration**: MDIServiceConfiguration ile kolay setup
- **Conditional Registration**: Platform, debug/release, custom conditions
- **Health Monitoring**: Service health checks ve reporting
- **Dependency Analysis**: Dependency graph ve validation

### Unity Integration
- **MonoBehaviour Support**: Automatic injection, property drawers
- **Editor Tools**: Setup wizard, monitor window, validation window
- **Code Generation**: Template-based service generator
- **Visual Debugging**: Inspector integration, dependency visualization
- **Bootstrap System**: Otomatik container kurulumu

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

### ✅ Tamamlanan Özellikler
- [x] Core DI System (Container, Registration, Resolution)
- [x] Unity Integration (MonoBehaviour, Editor tools)
- [x] Fluent API (Configuration, Helper methods)
- [x] Validation System (Real-time dependency checking)
- [x] Performance Monitoring (Metrics, Health checks)
- [x] Developer Tools (Setup wizard, Visual debugging)
- [x] Auto-Registration (Attribute-based discovery)
- [x] Code Generation (Template-based generators)

### 🚧 Gelecek Özellikler
- [ ] Signal System (Event-driven communication)
- [ ] Command Pattern (Sequential, Parallel execution)
- [ ] Function Pipeline (Data transformation chains)
- [ ] Advanced Scoping (Scene-based, custom scopes)
- [ ] Plugin System (Extensible architecture)
- [ ] Performance Optimization (IL generation, caching)
- [ ] Documentation (Comprehensive guides, examples)

---

**MDI+** - Unity'de dependency injection'ın geleceği! 🚀

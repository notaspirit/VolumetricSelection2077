using System;
using System.ComponentModel;
using VolumetricSelection2077.App.Models.SettingsDTO;
using VolumetricSelection2077.Core.Services;

namespace VolumetricSelection2077.App.Services;

public class AppCacheService : CacheService, IDisposable
{
    private readonly SettingsService _settingsService;

    private SettingsDTOV1? _subscribedData;

    public AppCacheService(SettingsService settingsService)
        : base(settingsService.Data.CacheDirectory,
            AppUtils.GetExeVersion(settingsService.Data.GameExecutable),
            settingsService.Data.CacheEnabled)
    {
        _settingsService = settingsService;

        _settingsService.PropertyChanged += OnSettingsPropertyChanged;
        SubscribeToData(_settingsService.Data);
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(SettingsService.Data))
            return;

        SubscribeToData(_settingsService.Data);
    }

    private void SubscribeToData(SettingsDTOV1 data)
    {
        if (_subscribedData != null)
            _subscribedData.PropertyChanged -= OnDataPropertyChanged;

        _subscribedData = data;
        _subscribedData.PropertyChanged += OnDataPropertyChanged;
    }

    private void OnDataPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(SettingsDTOV1.CacheEnabled))
            return;

        ClearCacheOnExit = _subscribedData!.CacheEnabled;
    }

    void IDisposable.Dispose()
    {
        _settingsService.PropertyChanged -= OnSettingsPropertyChanged;
        if (_subscribedData != null)
            _subscribedData.PropertyChanged -= OnDataPropertyChanged;
    }
}
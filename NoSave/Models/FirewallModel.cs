using NoSave.MVVM;
using NoSave.Services.Interfaces;

namespace NoSave.Models
{
    public class FirewallModel : ViewModelBase
    {
        private readonly IFirewallService _firewallService;
        public const string RuleName = "NoSave";

        private bool _isRuleActive;

        public bool IsRuleActive
        {
            get => _isRuleActive;
            private set
            {
                _isRuleActive = value;
                OnPropertyChanged();
            }
        }

        public FirewallModel(IFirewallService firewallService)
        {
            _firewallService = firewallService;
        }

        public void UpdateStatus()
        {
            IsRuleActive = _firewallService.CheckFirewallRule(RuleName);
        }

        /// <summary>Removes the block rule and refreshes the status. Throws if the rule could not be removed.</summary>
        public void RemoveBlock()
        {
            try
            {
                _firewallService.RemoveRule(RuleName);
            }
            finally
            {
                UpdateStatus();
            }
        }
    }
}

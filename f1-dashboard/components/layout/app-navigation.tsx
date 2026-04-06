const navigationItems = [
  { label: "Dashboard", href: "#" },
  { label: "Leaderboard", href: "#" },
  { label: "Track Map", href: "#" },
  { label: "Telemetry", href: "#" },
];

export function AppNavigation() {
  return (
    <nav className="app-navigation" aria-label="Primary">
      <div className="app-navigation__brand">
        <p className="eyebrow">F1 Dashboard</p>
        <strong>Race Control UI</strong>
      </div>

      <ul className="app-navigation__list">
        {navigationItems.map((item) => (
          <li key={item.label}>
            <a className="app-navigation__link" href={item.href}>
              {item.label}
            </a>
          </li>
        ))}
      </ul>
    </nav>
  );
}

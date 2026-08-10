import { NavLink } from "react-router-dom";
import { useBrowserApi } from "../../api/browser-api";
import { Badge } from "../ui/Badge";

interface NavigationProps {
  layout?: "rail" | "mobile";
}

export function Navigation({ layout = "rail" }: NavigationProps) {
  const { navigation } = useBrowserApi();
  const destinations = navigation?.destinations ?? [];

  const available = destinations.filter((destination) => destination.is_available);
  const planned = destinations.filter(
    (destination) => !destination.is_available && destination.tier === "p1",
  );

  const navLabel = layout === "rail" ? "Primary navigation" : "Mobile navigation";

  return (
    <nav aria-label={navLabel}>
      <ul className="nav-list">
        {available.map((destination) => (
          <li key={destination.destination_id}>
            <NavLink
              to={destination.route}
              end={destination.route === "/"}
              className={({ isActive }) =>
                ["nav-link", isActive ? "nav-link-active" : ""].filter(Boolean).join(" ")
              }
            >
              <span>{destination.label}</span>
              {destination.tier === "p1" ? <Badge variant="tier">P1</Badge> : null}
            </NavLink>
          </li>
        ))}
        {planned.map((destination) => (
          <li key={destination.destination_id}>
            <NavLink
              to={destination.route}
              className={({ isActive }) =>
                ["nav-link", isActive ? "nav-link-active" : ""].filter(Boolean).join(" ")
              }
            >
              <span>{destination.label}</span>
              <Badge variant="tier">P1</Badge>
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  );
}

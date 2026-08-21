import { useEffect, useId, useState } from "react";
import { Link } from "react-router-dom";
import {
  REQUIRED_SOURCE_CATEGORIES,
  resolveSelectedSources,
  sourceOptionIdentity,
  sourceOptionLabel,
  type ProductionActivityList,
  type ProductionSourceOption,
  type ProductionSourceRef,
} from "../api/production-assessment";
import { Alert } from "../components/ui/Alert";
import { Badge } from "../components/ui/Badge";
import { Button } from "../components/ui/Button";
import { Card, CardBody, CardHeader, CardTitle } from "../components/ui/Card";
import { ErrorSummary } from "../components/ui/ErrorSummary";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";
import { StatusPanel } from "../components/ui/StatusPanel";

function isAccessLoss(cause: unknown) {
  return cause instanceof Error && /access changed|expired/i.test(cause.message);
}

export interface AssessmentActivitiesPageProps {
  organizationId?: string;
  loadActivities: () => Promise<ProductionActivityList>;
  loadSourceOptions: () => Promise<{ sources: ProductionSourceOption[] }>;
  createActivity: (title: string, sources: Partial<Record<string, ProductionSourceRef>>) => Promise<string>;
  onCreated: (activityId: string) => void;
}

export function AssessmentActivitiesPage({
  organizationId,
  loadActivities,
  loadSourceOptions,
  createActivity,
  onCreated,
}: AssessmentActivitiesPageProps) {
  const titleId = useId();
  const [data, setData] = useState<ProductionActivityList | null>(null);
  const [sources, setSources] = useState<ProductionSourceOption[]>([]);
  const [selected, setSelected] = useState<Record<string, string>>({});
  const [title, setTitle] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);
  const [accessChanged, setAccessChanged] = useState(false);

  useEffect(() => {
    void (async () => {
      try {
        const list = await loadActivities();
        setData(list);
        try {
          const options = await loadSourceOptions();
          setSources(options.sources);
          const next: Record<string, string> = {};
          for (const category of REQUIRED_SOURCE_CATEGORIES) {
            const first = options.sources.find((source) => source.category === category);
            if (first) {
              next[category] = sourceOptionIdentity(first);
            }
          }

          setSelected(next);
        } catch {
          setSources([]);
        }

        setLoading(false);
      } catch (cause: unknown) {
        if (isAccessLoss(cause)) {
          setAccessChanged(true);
        } else {
          setError(cause instanceof Error ? cause.message : "Could not load activities");
        }

        setLoading(false);
      }
    })();
  }, [loadActivities, loadSourceOptions]);

  if (loading) {
    return <ProtectedLoading label="Loading activities…" />;
  }

  if (accessChanged) {
    return (
      <StatusPanel title="Your access changed" variant="danger">
        <p>Protected setup values were removed. Return to Home or sign in again.</p>
      </StatusPanel>
    );
  }

  if (error && !data) {
    return <Alert variant="danger" title="Could not load activities">{error}</Alert>;
  }

  const canCreate = data?.permitted_actions.includes("create_assessment") ?? false;
  const missingCategory = REQUIRED_SOURCE_CATEGORIES.find((category) => !sources.some((source) => source.category === category));

  return (
    <div>
      <header className="page-header">
        <h1>Activities</h1>
        <p>Create and resume Assessment Campaign drafts{organizationId ? ` for organization ${organizationId}` : ""}.</p>
      </header>

      {canCreate ? (
        <section className="page-section" aria-labelledby="create-heading">
          <h2 id="create-heading">Create assessment Campaign</h2>
          {missingCategory ? (
            <Alert variant="info" title={`No permitted ${missingCategory.replaceAll("_", " ")} revisions are available`}>
              A ready source set is required before a draft can be created.
            </Alert>
          ) : (
            <form
              className="stack"
              onSubmit={(event) => {
                event.preventDefault();
                setCreating(true);
                const chosen = resolveSelectedSources(sources, selected, REQUIRED_SOURCE_CATEGORIES);

                void createActivity(title, chosen)
                  .then((activityId) => {
                    onCreated(activityId);
                  })
                  .catch((cause: unknown) => {
                    if (isAccessLoss(cause)) {
                      setData(null);
                      setSources([]);
                      setSelected({});
                      setAccessChanged(true);
                      return;
                    }

                    setError("The Campaign could not be created.");
                    setCreating(false);
                  });
              }}
            >
              <label htmlFor={titleId}>Campaign title</label>
              <input
                id={titleId}
                value={title}
                onChange={(event) => {
                  setTitle(event.target.value);
                }}
                required
                maxLength={200}
              />
              {REQUIRED_SOURCE_CATEGORIES.map((category) => {
                const options = sources.filter((source) => source.category === category);
                const fieldId = `${titleId}-${category}`;
                return (
                  <div key={category} className="field">
                    <label className="field-label" htmlFor={fieldId}>{category.replaceAll("_", " ")}</label>
                    <select
                      id={fieldId}
                      value={selected[category] ?? ""}
                      onChange={(event) => {
                        setSelected((current) => ({ ...current, [category]: event.target.value }));
                      }}
                    >
                      {options.length === 0 ? <option value="">Unavailable</option> : null}
                      {options.map((option) => (
                        <option key={sourceOptionIdentity(option)} value={sourceOptionIdentity(option)}>
                          {sourceOptionLabel(option)}
                        </option>
                      ))}
                    </select>
                  </div>
                );
              })}
              {error ? <ErrorSummary title="Correct the following" errors={[error]} /> : null}
              <Button type="submit" disabled={creating || Boolean(missingCategory)}>
                {creating ? "Creating…" : "Create assessment Campaign"}
              </Button>
            </form>
          )}
        </section>
      ) : null}

      <section className="page-section" aria-labelledby="activities-list-heading">
        <h2 id="activities-list-heading">Activity list</h2>
        {data?.activities.length === 0 ? (
          <p className="empty-state">No activities are available.</p>
        ) : (
          <ul className="stack" aria-label="Activities">
            {data?.activities.map((activity) => (
              <li key={activity.activity_id}>
                <Card interactive>
                  <Link className="work-item-link" to={`/activities/${activity.activity_id}/setup`}>
                    <CardHeader>
                      <CardTitle>{activity.title}</CardTitle>
                    </CardHeader>
                    <CardBody>
                      <Badge variant={activity.has_activated_cohort ? "info" : "default"}>
                        {activity.has_activated_cohort ? "Activated" : "Draft"}
                      </Badge>
                    </CardBody>
                  </Link>
                </Card>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}

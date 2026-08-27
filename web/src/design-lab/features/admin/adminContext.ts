import { useOutletContext } from "react-router-dom";
import type { Dispatch, SetStateAction } from "react";
import type { Campaign } from "../../data/types";

export type AdminOutletContext = {
  campaigns: Campaign[];
  campaign: Campaign | undefined;
  campaignId: string | null;
  setCampaigns: Dispatch<SetStateAction<Campaign[]>>;
  setCampaignId: (id: string | null) => void;
  announce: (message: string) => void;
  pushToast: (notice: { label: string; copy: string; attention?: boolean }) => void;
  sealing: boolean;
  setSealing: (sealing: boolean) => void;
};

export function useAdminContext() {
  return useOutletContext<AdminOutletContext>();
}

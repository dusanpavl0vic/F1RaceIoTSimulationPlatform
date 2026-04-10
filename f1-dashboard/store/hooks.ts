import { useDispatch, useSelector, useStore } from "react-redux";
import type { AppDispatch, AppStore, RootState } from "@/store";

export const useAppDispatch = () => useDispatch<AppDispatch>();
export const useAppSelector: <TSelected>(
  selector: (state: RootState) => TSelected,
) => TSelected = useSelector;
export const useAppStore = () => useStore() as AppStore;

export interface MarketArea {
  id: number;
  country: string;
  district: string;
  municipality: string;
  town: string;
  zone: string | null;
  notes: string | null;
}

// What we send to GET /api/MarketArea/GetAll (as query string).
export interface MarketAreaQuery {
  search?: string;
  pageNumber: number;
  pageSize: number;
}

// One page of market areas, as the API returns it.
export interface MarketAreaPage {
  items: MarketArea[];
  pageNumber: number;
  pageSize: number;
  totalRecords: number;
}

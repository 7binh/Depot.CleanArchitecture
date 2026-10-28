/** One page of a list, as the API sends it. */
export interface Paged<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

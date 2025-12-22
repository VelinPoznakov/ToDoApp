export interface Todo {
  id: number;
  name: string;
  description?: string;
}

export interface SearchBarProps {
  value: string;
  setValue: (value: string) => void;
}
